using EnterpriseCsharpReverseProxy.ControlPlane.Models;
using EnterpriseCsharpReverseProxy.ControlPlane.Services;
using Xunit;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Tests.Services;

public class InMemoryConfigurationStoreTests
{
    private readonly InMemoryConfigurationStore _store = new();

    // ── Routes ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertRoute_ShouldAddRoute()
    {
        var route = new RouteConfig { RouteId = "route1", ClusterId = "c1", Match = new RouteMatch { Path = "/api/{**catch-all}" } };
        await _store.UpsertRouteAsync(route);

        var fetched = await _store.GetRouteAsync("route1");
        Assert.NotNull(fetched);
        Assert.Equal("c1", fetched.ClusterId);
    }

    [Fact]
    public async Task UpsertRoute_BumpsVersion()
    {
        var v0 = _store.CurrentVersion;
        await _store.UpsertRouteAsync(new RouteConfig { RouteId = "r", ClusterId = "c", Match = new RouteMatch { Path = "/" } });
        Assert.True(_store.CurrentVersion > v0);
    }

    [Fact]
    public async Task DeleteRoute_RemovesRoute()
    {
        await _store.UpsertRouteAsync(new RouteConfig { RouteId = "r2", ClusterId = "c", Match = new RouteMatch { Path = "/" } });
        var deleted = await _store.DeleteRouteAsync("r2");
        Assert.True(deleted);
        Assert.Null(await _store.GetRouteAsync("r2"));
    }

    [Fact]
    public async Task DeleteRoute_ReturnsFalseWhenNotFound()
    {
        var deleted = await _store.DeleteRouteAsync("no-such-route");
        Assert.False(deleted);
    }

    // ── Clusters ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertCluster_ShouldAddCluster()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "backend",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["d1"] = new DestinationConfig { DestinationId = "d1", Address = "https://example.com" }
            }
        };
        await _store.UpsertClusterAsync(cluster);
        var fetched = await _store.GetClusterAsync("backend");
        Assert.NotNull(fetched);
        Assert.Single(fetched.Destinations);
    }

    [Fact]
    public async Task UpsertDestination_ThrowsForUnknownCluster()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _store.UpsertDestinationAsync("ghost", new DestinationConfig { DestinationId = "d", Address = "http://x.com" }));
    }

    [Fact]
    public async Task DeleteDestination_RemovesDestination()
    {
        await _store.UpsertClusterAsync(new ClusterConfig
        {
            ClusterId = "cl",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["dst"] = new DestinationConfig { DestinationId = "dst", Address = "http://a.com" }
            }
        });
        var removed = await _store.DeleteDestinationAsync("cl", "dst");
        Assert.True(removed);
        var cluster = await _store.GetClusterAsync("cl");
        Assert.Empty(cluster!.Destinations);
    }

    // ── Snapshot ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSnapshot_ReturnsCurrentVersion()
    {
        await _store.UpsertRouteAsync(new RouteConfig { RouteId = "r", ClusterId = "c", Match = new RouteMatch { Path = "/" } });
        var snapshot = await _store.GetSnapshotAsync();
        Assert.Equal(_store.CurrentVersion, snapshot.Version);
    }
}

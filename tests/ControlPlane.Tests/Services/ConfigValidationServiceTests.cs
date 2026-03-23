using EnterpriseCsharpReverseProxy.ControlPlane.Models;
using EnterpriseCsharpReverseProxy.ControlPlane.Services;
using Xunit;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Tests.Services;

public class ConfigValidationServiceTests
{
    private readonly ConfigValidationService _svc = new();

    // ── Route validation ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateRoute_Valid_ReturnsNoErrors()
    {
        var route = new RouteConfig
        {
            RouteId = "r1",
            ClusterId = "c1",
            Match = new RouteMatch { Path = "/api/{**catch-all}" }
        };
        Assert.True(_svc.ValidateRoute(route).IsValid);
    }

    [Fact]
    public void ValidateRoute_MissingRouteId_ReturnsError()
    {
        var route = new RouteConfig { ClusterId = "c1", Match = new RouteMatch { Path = "/" } };
        Assert.False(_svc.ValidateRoute(route).IsValid);
    }

    [Fact]
    public void ValidateRoute_MissingPathAndHost_ReturnsError()
    {
        var route = new RouteConfig { RouteId = "r", ClusterId = "c", Match = new RouteMatch() };
        var result = _svc.ValidateRoute(route);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Path") || e.Contains("Host"));
    }

    // ── Cluster validation ───────────────────────────────────────────────────

    [Fact]
    public void ValidateCluster_Valid_ReturnsNoErrors()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "c1",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["d1"] = new DestinationConfig { DestinationId = "d1", Address = "https://example.com" }
            }
        };
        Assert.True(_svc.ValidateCluster(cluster).IsValid);
    }

    [Fact]
    public void ValidateCluster_InvalidDestinationAddress_ReturnsError()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "c1",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["d1"] = new DestinationConfig { DestinationId = "d1", Address = "not-a-url" }
            }
        };
        var result = _svc.ValidateCluster(cluster);
        Assert.False(result.IsValid);
    }
}

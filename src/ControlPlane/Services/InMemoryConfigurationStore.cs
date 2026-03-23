using System.Collections.Concurrent;
using EnterpriseCsharpReverseProxy.ControlPlane.Models;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Services;

/// <summary>
/// Thread-safe in-memory configuration store.
/// Replace with a database-backed implementation for production use.
/// </summary>
public class InMemoryConfigurationStore : IConfigurationStore
{
    private readonly ConcurrentDictionary<string, RouteConfig> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ClusterConfig> _clusters = new(StringComparer.OrdinalIgnoreCase);
    private long _version;

    public long CurrentVersion => Volatile.Read(ref _version);

    // ── Routes ──────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<RouteConfig>> GetAllRoutesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RouteConfig>>(_routes.Values.OrderBy(r => r.Order).ToList());

    public Task<RouteConfig?> GetRouteAsync(string routeId, CancellationToken ct = default)
        => Task.FromResult(_routes.TryGetValue(routeId, out var route) ? route : null);

    public Task<RouteConfig> UpsertRouteAsync(RouteConfig route, CancellationToken ct = default)
    {
        route.UpdatedAt = DateTimeOffset.UtcNow;
        _routes.AddOrUpdate(route.RouteId, _ =>
        {
            route.CreatedAt = route.UpdatedAt;
            return route;
        }, (_, existing) =>
        {
            route.CreatedAt = existing.CreatedAt;
            return route;
        });
        BumpVersion();
        return Task.FromResult(route);
    }

    public Task<bool> DeleteRouteAsync(string routeId, CancellationToken ct = default)
    {
        var removed = _routes.TryRemove(routeId, out _);
        if (removed) BumpVersion();
        return Task.FromResult(removed);
    }

    // ── Clusters ────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<ClusterConfig>> GetAllClustersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ClusterConfig>>(_clusters.Values.ToList());

    public Task<ClusterConfig?> GetClusterAsync(string clusterId, CancellationToken ct = default)
        => Task.FromResult(_clusters.TryGetValue(clusterId, out var cluster) ? cluster : null);

    public Task<ClusterConfig> UpsertClusterAsync(ClusterConfig cluster, CancellationToken ct = default)
    {
        cluster.UpdatedAt = DateTimeOffset.UtcNow;
        _clusters.AddOrUpdate(cluster.ClusterId, _ =>
        {
            cluster.CreatedAt = cluster.UpdatedAt;
            return cluster;
        }, (_, existing) =>
        {
            cluster.CreatedAt = existing.CreatedAt;
            return cluster;
        });
        BumpVersion();
        return Task.FromResult(cluster);
    }

    public Task<bool> DeleteClusterAsync(string clusterId, CancellationToken ct = default)
    {
        var removed = _clusters.TryRemove(clusterId, out _);
        if (removed) BumpVersion();
        return Task.FromResult(removed);
    }

    // ── Destinations ─────────────────────────────────────────────────────────

    public Task<DestinationConfig> UpsertDestinationAsync(string clusterId, DestinationConfig destination, CancellationToken ct = default)
    {
        if (!_clusters.TryGetValue(clusterId, out var cluster))
            throw new KeyNotFoundException($"Cluster '{clusterId}' not found.");

        cluster.Destinations[destination.DestinationId] = destination;
        cluster.UpdatedAt = DateTimeOffset.UtcNow;
        BumpVersion();
        return Task.FromResult(destination);
    }

    public Task<bool> DeleteDestinationAsync(string clusterId, string destinationId, CancellationToken ct = default)
    {
        if (!_clusters.TryGetValue(clusterId, out var cluster))
            return Task.FromResult(false);

        var removed = cluster.Destinations.Remove(destinationId);
        if (removed)
        {
            cluster.UpdatedAt = DateTimeOffset.UtcNow;
            BumpVersion();
        }
        return Task.FromResult(removed);
    }

    // ── Snapshot ─────────────────────────────────────────────────────────────

    public Task<ProxySnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        var snapshot = new ProxySnapshot
        {
            Version = CurrentVersion,
            Routes = _routes.Values.OrderBy(r => r.Order).ToList(),
            Clusters = _clusters.Values.ToList()
        };
        return Task.FromResult(snapshot);
    }

    private void BumpVersion() => Interlocked.Increment(ref _version);
}

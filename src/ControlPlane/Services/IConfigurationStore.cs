using EnterpriseCsharpReverseProxy.ControlPlane.Models;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Services;

public interface IConfigurationStore
{
    // ── Routes ──────────────────────────────────────────────────────────────
    Task<IReadOnlyList<RouteConfig>> GetAllRoutesAsync(CancellationToken ct = default);
    Task<RouteConfig?> GetRouteAsync(string routeId, CancellationToken ct = default);
    Task<RouteConfig> UpsertRouteAsync(RouteConfig route, CancellationToken ct = default);
    Task<bool> DeleteRouteAsync(string routeId, CancellationToken ct = default);

    // ── Clusters ────────────────────────────────────────────────────────────
    Task<IReadOnlyList<ClusterConfig>> GetAllClustersAsync(CancellationToken ct = default);
    Task<ClusterConfig?> GetClusterAsync(string clusterId, CancellationToken ct = default);
    Task<ClusterConfig> UpsertClusterAsync(ClusterConfig cluster, CancellationToken ct = default);
    Task<bool> DeleteClusterAsync(string clusterId, CancellationToken ct = default);

    // ── Destinations ─────────────────────────────────────────────────────────
    Task<DestinationConfig> UpsertDestinationAsync(string clusterId, DestinationConfig destination, CancellationToken ct = default);
    Task<bool> DeleteDestinationAsync(string clusterId, string destinationId, CancellationToken ct = default);

    // ── Snapshot ─────────────────────────────────────────────────────────────
    Task<ProxySnapshot> GetSnapshotAsync(CancellationToken ct = default);
    long CurrentVersion { get; }
}

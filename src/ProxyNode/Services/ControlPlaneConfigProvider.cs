using EnterpriseCsharpReverseProxy.ControlPlane.Models;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace EnterpriseCsharpReverseProxy.ProxyNode.Services;

/// <summary>
/// YARP IProxyConfigProvider implementation that reads configuration
/// from the control plane and supports hot-reload via IChangeToken.
/// </summary>
public class ControlPlaneConfigProvider : IProxyConfigProvider, IDisposable
{
    private volatile InMemoryConfig _config = new([], []);
    private CancellationTokenSource _cts = new();
    private readonly ILogger<ControlPlaneConfigProvider> _logger;

    public ControlPlaneConfigProvider(ILogger<ControlPlaneConfigProvider> logger)
    {
        _logger = logger;
    }

    public IProxyConfig GetConfig() => _config;

    /// <summary>Apply a new snapshot and signal YARP to reload.</summary>
    public void Update(ProxySnapshot snapshot)
    {
        var routes  = MapRoutes(snapshot.Routes);
        var clusters = MapClusters(snapshot.Clusters);

        var oldCts = _cts;
        _cts = new CancellationTokenSource();
        _config = new InMemoryConfig(routes, clusters, _cts.Token);

        _logger.LogInformation(
            "YARP config updated: {Routes} routes, {Clusters} clusters (version={Version})",
            routes.Count, clusters.Count, snapshot.Version);

        oldCts.Cancel();
        oldCts.Dispose();
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    private static List<RouteConfig> MapRoutes(IEnumerable<ControlPlane.Models.RouteConfig> models)
        => models.Select(r => new RouteConfig
        {
            RouteId = r.RouteId,
            ClusterId = r.ClusterId,
            Order = r.Order,
            Match = new RouteMatch
            {
                Hosts = r.Match?.Hosts,
                Path  = r.Match?.Path,
                Methods = r.Match?.Methods,
                Headers = r.Match?.Headers?.Select(h => new RouteHeader
                {
                    Name   = h.Name,
                    Values = h.Values,
                    Mode   = Enum.Parse<HeaderMatchMode>(h.Mode.ToString()),
                    IsCaseSensitive = h.IsCaseSensitive
                }).ToList()
            },
            Metadata = r.Metadata
        }).ToList();

    private static List<ClusterConfig> MapClusters(IEnumerable<ControlPlane.Models.ClusterConfig> models)
        => models.Select(c => new ClusterConfig
        {
            ClusterId = c.ClusterId,
            LoadBalancingPolicy = c.LoadBalancing.Policy.ToString(),
            Destinations = c.Destinations.ToDictionary(
                kvp => kvp.Key,
                kvp => new DestinationConfig { Address = kvp.Value.Address }),
            Metadata = c.Metadata,
            HealthCheck = c.HealthCheck is null ? null : new HealthCheckConfig
            {
                Active = c.HealthCheck.Active is null ? null : new ActiveHealthCheckConfig
                {
                    Enabled  = c.HealthCheck.Active.Enabled,
                    Interval = c.HealthCheck.Active.Interval,
                    Timeout  = c.HealthCheck.Active.Timeout,
                    Policy   = c.HealthCheck.Active.Policy,
                    Path     = c.HealthCheck.Active.Path
                },
                Passive = c.HealthCheck.Passive is null ? null : new PassiveHealthCheckConfig
                {
                    Enabled            = c.HealthCheck.Passive.Enabled,
                    Policy             = c.HealthCheck.Passive.Policy,
                    ReactivationPeriod = c.HealthCheck.Passive.ReactivationPeriod
                }
            }
        }).ToList();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

/// <summary>
/// Simple IProxyConfig backed by in-memory lists.
/// </summary>
internal sealed class InMemoryConfig : IProxyConfig
{
    private readonly CancellationToken _changeToken;

    public InMemoryConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters,
        CancellationToken changeToken = default)
    {
        Routes       = routes;
        Clusters     = clusters;
        _changeToken = changeToken;
    }

    public IReadOnlyList<RouteConfig> Routes { get; }
    public IReadOnlyList<ClusterConfig> Clusters { get; }
    public IChangeToken ChangeToken => new CancellationChangeToken(_changeToken);
}

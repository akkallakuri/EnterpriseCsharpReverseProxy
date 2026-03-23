namespace EnterpriseCsharpReverseProxy.ControlPlane.Models;

/// <summary>
/// Full configuration snapshot vended to proxy nodes.
/// </summary>
public class ProxySnapshot
{
    public long Version { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<RouteConfig> Routes { get; set; } = new();
    public List<ClusterConfig> Clusters { get; set; } = new();
}

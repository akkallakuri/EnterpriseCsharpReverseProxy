namespace EnterpriseCsharpReverseProxy.ControlPlane.Models;

public class ClusterConfig
{
    public string ClusterId { get; set; } = string.Empty;
    public LoadBalancingConfig LoadBalancing { get; set; } = new();
    public HealthCheckConfig? HealthCheck { get; set; }
    public SessionAffinityConfig? SessionAffinity { get; set; }
    public HttpClientConfig? HttpClient { get; set; }
    public Dictionary<string, DestinationConfig> Destinations { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SessionAffinityConfig
{
    public bool Enabled { get; set; }
    public string Policy { get; set; } = "Cookie";
    public string? FailurePolicy { get; set; } = "Redistribute";
    public string? AffinityKeyName { get; set; } = ".Proxy.Affinity";
}

public class HttpClientConfig
{
    public bool? DangerousAcceptAnyServerCertificate { get; set; }
    public int? MaxConnectionsPerServer { get; set; }
    public TimeSpan? ActivityTimeout { get; set; }
    public bool? EnableMultipleHttp2Connections { get; set; }
}

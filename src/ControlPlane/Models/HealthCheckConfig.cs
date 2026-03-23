namespace EnterpriseCsharpReverseProxy.ControlPlane.Models;

public class HealthCheckConfig
{
    public ActiveHealthCheckConfig? Active { get; set; }
    public PassiveHealthCheckConfig? Passive { get; set; }
}

public class ActiveHealthCheckConfig
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public string Policy { get; set; } = "ConsecutiveFailures";
    public string? Path { get; set; } = "/health";
}

public class PassiveHealthCheckConfig
{
    public bool Enabled { get; set; } = true;
    public string Policy { get; set; } = "TransportFailureRate";
    public TimeSpan ReactivationPeriod { get; set; } = TimeSpan.FromMinutes(2);
}

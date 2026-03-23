namespace EnterpriseCsharpReverseProxy.ControlPlane.Models;

public class DestinationConfig
{
    public string DestinationId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Health { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

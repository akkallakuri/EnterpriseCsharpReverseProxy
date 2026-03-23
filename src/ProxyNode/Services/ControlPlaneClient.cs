using System.Net.Http.Json;
using EnterpriseCsharpReverseProxy.ControlPlane.Models;

namespace EnterpriseCsharpReverseProxy.ProxyNode.Services;

/// <summary>
/// HTTP client for the control plane API.
/// </summary>
public class ControlPlaneClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ControlPlaneClient> _logger;

    public ControlPlaneClient(HttpClient http, ILogger<ControlPlaneClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ProxySnapshot?> GetSnapshotAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ProxySnapshot>("api/v1/config/snapshot", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch snapshot from control plane.");
            return null;
        }
    }

    /// <summary>
    /// Long-poll: returns a new snapshot when the version advances past <paramref name="knownVersion"/>,
    /// or null on timeout / error.
    /// </summary>
    public async Task<ProxySnapshot?> WatchAsync(long knownVersion, int timeoutSeconds = 30, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(
                $"api/v1/config/watch?knownVersion={knownVersion}&timeoutSeconds={timeoutSeconds}", ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProxySnapshot>(cancellationToken: ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during watch poll.");
            return null;
        }
    }
}

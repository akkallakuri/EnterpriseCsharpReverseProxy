using EnterpriseCsharpReverseProxy.ControlPlane.Models;
using EnterpriseCsharpReverseProxy.ControlPlane.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Controllers;

[ApiController]
[Route("api/v1/config")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly IConfigurationStore _store;
    private readonly IConfigChangeNotifier _notifier;

    public ConfigController(IConfigurationStore store, IConfigChangeNotifier notifier)
    {
        _store = store;
        _notifier = notifier;
    }

    /// <summary>
    /// Returns the full configuration snapshot (routes + clusters).
    /// Proxy nodes call this on startup and whenever they detect a version mismatch.
    /// </summary>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(ProxySnapshot), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshot(CancellationToken ct)
        => Ok(await _store.GetSnapshotAsync(ct));

    /// <summary>
    /// Long-poll endpoint: blocks until the config version advances past
    /// <paramref name="knownVersion"/>, then returns the new snapshot.
    /// Proxy nodes use this to get near-real-time updates without polling.
    /// </summary>
    [HttpGet("watch")]
    [ProducesResponseType(typeof(ProxySnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> Watch(
        [FromQuery] long knownVersion = -1,
        [FromQuery] int timeoutSeconds = 30,
        CancellationToken ct = default)
    {
        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 120);

        if (_store.CurrentVersion != knownVersion)
            return Ok(await _store.GetSnapshotAsync(ct));

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await Task.Delay(Timeout.Infinite, CancellationTokenSource
                .CreateLinkedTokenSource(timeoutCts.Token, _notifier.GetChangeToken())
                .Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Either the watch token fired (config changed) or timeout elapsed
        }

        if (_store.CurrentVersion == knownVersion)
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(await _store.GetSnapshotAsync(ct));
    }

    /// <summary>Current config version number.</summary>
    [HttpGet("version")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetVersion()
        => Ok(new { version = _store.CurrentVersion });

    /// <summary>Replace the entire configuration in one atomic operation.</summary>
    [HttpPut("snapshot")]
    [ProducesResponseType(typeof(ProxySnapshot), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplaceSnapshot([FromBody] ProxySnapshot snapshot, CancellationToken ct)
    {
        foreach (var cluster in snapshot.Clusters)
            await _store.UpsertClusterAsync(cluster, ct);

        foreach (var route in snapshot.Routes)
            await _store.UpsertRouteAsync(route, ct);

        _notifier.NotifyChanged();
        return Ok(await _store.GetSnapshotAsync(ct));
    }
}

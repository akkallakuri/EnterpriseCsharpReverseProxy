using EnterpriseCsharpReverseProxy.ControlPlane.Models;
using EnterpriseCsharpReverseProxy.ControlPlane.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Controllers;

[ApiController]
[Route("api/v1/clusters")]
[Produces("application/json")]
public class ClustersController : ControllerBase
{
    private readonly IConfigurationStore _store;
    private readonly ConfigValidationService _validator;
    private readonly IConfigChangeNotifier _notifier;
    private readonly ILogger<ClustersController> _logger;

    public ClustersController(
        IConfigurationStore store,
        ConfigValidationService validator,
        IConfigChangeNotifier notifier,
        ILogger<ClustersController> logger)
    {
        _store = store;
        _validator = validator;
        _notifier = notifier;
        _logger = logger;
    }

    /// <summary>List all clusters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClusterConfig>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _store.GetAllClustersAsync(ct));

    /// <summary>Get a single cluster by ID.</summary>
    [HttpGet("{clusterId}")]
    [ProducesResponseType(typeof(ClusterConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string clusterId, CancellationToken ct)
    {
        var cluster = await _store.GetClusterAsync(clusterId, ct);
        return cluster is null ? NotFound() : Ok(cluster);
    }

    /// <summary>Create or replace a cluster.</summary>
    [HttpPut("{clusterId}")]
    [ProducesResponseType(typeof(ClusterConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(string clusterId, [FromBody] ClusterConfig cluster, CancellationToken ct)
    {
        cluster.ClusterId = clusterId;

        var validation = _validator.ValidateCluster(cluster);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => "cluster", e => new[] { e })));

        var saved = await _store.UpsertClusterAsync(cluster, ct);
        _notifier.NotifyChanged();
        _logger.LogInformation("Cluster {ClusterId} upserted (version={Version})", clusterId, _store.CurrentVersion);
        return Ok(saved);
    }

    /// <summary>Delete a cluster.</summary>
    [HttpDelete("{clusterId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string clusterId, CancellationToken ct)
    {
        var deleted = await _store.DeleteClusterAsync(clusterId, ct);
        if (!deleted) return NotFound();
        _notifier.NotifyChanged();
        _logger.LogInformation("Cluster {ClusterId} deleted (version={Version})", clusterId, _store.CurrentVersion);
        return NoContent();
    }

    // ── Destinations ─────────────────────────────────────────────────────────

    /// <summary>Add or replace a destination within a cluster.</summary>
    [HttpPut("{clusterId}/destinations/{destinationId}")]
    [ProducesResponseType(typeof(DestinationConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertDestination(
        string clusterId, string destinationId,
        [FromBody] DestinationConfig destination, CancellationToken ct)
    {
        destination.DestinationId = destinationId;

        var validation = _validator.ValidateDestination(destination);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => "destination", e => new[] { e })));

        try
        {
            var saved = await _store.UpsertDestinationAsync(clusterId, destination, ct);
            _notifier.NotifyChanged();
            _logger.LogInformation("Destination {DestinationId} upserted in cluster {ClusterId}", destinationId, clusterId);
            return Ok(saved);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Cluster '{clusterId}' not found." });
        }
    }

    /// <summary>Remove a destination from a cluster.</summary>
    [HttpDelete("{clusterId}/destinations/{destinationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDestination(string clusterId, string destinationId, CancellationToken ct)
    {
        var deleted = await _store.DeleteDestinationAsync(clusterId, destinationId, ct);
        if (!deleted) return NotFound();
        _notifier.NotifyChanged();
        _logger.LogInformation("Destination {DestinationId} deleted from cluster {ClusterId}", destinationId, clusterId);
        return NoContent();
    }
}

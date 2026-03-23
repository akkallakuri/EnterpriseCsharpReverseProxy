using EnterpriseCsharpReverseProxy.ControlPlane.Models;
using EnterpriseCsharpReverseProxy.ControlPlane.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Controllers;

[ApiController]
[Route("api/v1/routes")]
[Produces("application/json")]
public class RoutesController : ControllerBase
{
    private readonly IConfigurationStore _store;
    private readonly ConfigValidationService _validator;
    private readonly IConfigChangeNotifier _notifier;
    private readonly ILogger<RoutesController> _logger;

    public RoutesController(
        IConfigurationStore store,
        ConfigValidationService validator,
        IConfigChangeNotifier notifier,
        ILogger<RoutesController> logger)
    {
        _store = store;
        _validator = validator;
        _notifier = notifier;
        _logger = logger;
    }

    /// <summary>List all routes ordered by priority.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RouteConfig>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _store.GetAllRoutesAsync(ct));

    /// <summary>Get a single route by ID.</summary>
    [HttpGet("{routeId}")]
    [ProducesResponseType(typeof(RouteConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string routeId, CancellationToken ct)
    {
        var route = await _store.GetRouteAsync(routeId, ct);
        return route is null ? NotFound() : Ok(route);
    }

    /// <summary>Create or replace a route.</summary>
    [HttpPut("{routeId}")]
    [ProducesResponseType(typeof(RouteConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(string routeId, [FromBody] RouteConfig route, CancellationToken ct)
    {
        route.RouteId = routeId;

        var validation = _validator.ValidateRoute(route);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => "route", e => new[] { e })));

        var saved = await _store.UpsertRouteAsync(route, ct);
        _notifier.NotifyChanged();
        _logger.LogInformation("Route {RouteId} upserted (version={Version})", routeId, _store.CurrentVersion);
        return Ok(saved);
    }

    /// <summary>Delete a route.</summary>
    [HttpDelete("{routeId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string routeId, CancellationToken ct)
    {
        var deleted = await _store.DeleteRouteAsync(routeId, ct);
        if (!deleted) return NotFound();
        _notifier.NotifyChanged();
        _logger.LogInformation("Route {RouteId} deleted (version={Version})", routeId, _store.CurrentVersion);
        return NoContent();
    }
}

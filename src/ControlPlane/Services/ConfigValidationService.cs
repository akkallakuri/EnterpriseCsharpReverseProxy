using EnterpriseCsharpReverseProxy.ControlPlane.Models;

namespace EnterpriseCsharpReverseProxy.ControlPlane.Services;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
}

public class ConfigValidationService
{
    public ValidationResult ValidateRoute(RouteConfig route)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(route.RouteId))
            result.Errors.Add("RouteId is required.");

        if (string.IsNullOrWhiteSpace(route.ClusterId))
            result.Errors.Add("ClusterId is required.");

        if (route.Match is null)
            result.Errors.Add("Match is required.");
        else if (string.IsNullOrWhiteSpace(route.Match.Path) && (route.Match.Hosts is null || route.Match.Hosts.Count == 0))
            result.Errors.Add("Match must specify at least a Path or a Host.");

        return result;
    }

    public ValidationResult ValidateCluster(ClusterConfig cluster)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(cluster.ClusterId))
            result.Errors.Add("ClusterId is required.");

        foreach (var (id, dest) in cluster.Destinations)
        {
            if (string.IsNullOrWhiteSpace(dest.Address))
                result.Errors.Add($"Destination '{id}' must have a non-empty Address.");

            if (!Uri.TryCreate(dest.Address, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
                result.Errors.Add($"Destination '{id}' has an invalid Address '{dest.Address}'. Must be an absolute http/https URL.");
        }

        return result;
    }

    public ValidationResult ValidateDestination(DestinationConfig destination)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(destination.DestinationId))
            result.Errors.Add("DestinationId is required.");

        if (string.IsNullOrWhiteSpace(destination.Address))
            result.Errors.Add("Address is required.");
        else if (!Uri.TryCreate(destination.Address, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
            result.Errors.Add($"Address '{destination.Address}' must be an absolute http/https URL.");

        return result;
    }
}

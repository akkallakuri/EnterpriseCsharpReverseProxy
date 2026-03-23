namespace EnterpriseCsharpReverseProxy.ControlPlane.Middleware;

/// <summary>
/// Simple API-key authentication middleware.
/// Configure the key via the "ControlPlane:ApiKey" setting.
/// Bypass is allowed for health-check endpoints.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string ApiKeyQuery  = "api_key";

    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["ControlPlane:ApiKey"]
                  ?? throw new InvalidOperationException("ControlPlane:ApiKey is not configured.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bypass auth for health / liveness probes
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        if (!TryExtractKey(context.Request, out var providedKey) ||
            !string.Equals(providedKey, _apiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });
            return;
        }

        await _next(context);
    }

    private static bool TryExtractKey(HttpRequest request, out string? key)
    {
        if (request.Headers.TryGetValue(ApiKeyHeader, out var headerVal))
        {
            key = headerVal.ToString();
            return true;
        }
        if (request.Query.TryGetValue(ApiKeyQuery, out var queryVal))
        {
            key = queryVal.ToString();
            return true;
        }
        key = null;
        return false;
    }
}

public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder app)
        => app.UseMiddleware<ApiKeyAuthMiddleware>();
}

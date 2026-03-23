using EnterpriseCsharpReverseProxy.ControlPlane.Middleware;
using EnterpriseCsharpReverseProxy.ControlPlane.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConfigurationStore, InMemoryConfigurationStore>();
builder.Services.AddSingleton<IConfigChangeNotifier, ConfigChangeNotifier>();
builder.Services.AddSingleton<ConfigValidationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Enterprise Reverse Proxy — Control Plane",
        Version = "v1",
        Description = "Manage routes, clusters, and destinations for the proxy fleet."
    });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Description = "API key for control plane access"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

// ── App ──────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Control Plane v1");
    c.RoutePrefix = string.Empty;  // Serve Swagger at root
});

app.UseApiKeyAuth();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

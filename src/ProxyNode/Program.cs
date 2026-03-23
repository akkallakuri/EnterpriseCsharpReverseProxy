using EnterpriseCsharpReverseProxy.ProxyNode.Services;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

var controlPlaneUrl = builder.Configuration["ProxyNode:ControlPlaneUrl"]
    ?? throw new InvalidOperationException("ProxyNode:ControlPlaneUrl is not configured.");

var apiKey = builder.Configuration["ProxyNode:ControlPlaneApiKey"]
    ?? throw new InvalidOperationException("ProxyNode:ControlPlaneApiKey is not configured.");

// ── Control plane client ──────────────────────────────────────────────────
builder.Services.AddHttpClient<ControlPlaneClient>(c =>
{
    c.BaseAddress = new Uri(controlPlaneUrl);
    c.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    c.Timeout = TimeSpan.FromSeconds(90);  // long enough for watch requests
});

// ── Config provider & watcher ─────────────────────────────────────────────
builder.Services.AddSingleton<ControlPlaneConfigProvider>();
builder.Services.AddHostedService<ConfigWatcherService>();

// ── YARP ──────────────────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromMemory([], []);  // Start empty; watcher will populate

// Override YARP's provider with our control-plane-backed implementation
builder.Services.AddSingleton<IProxyConfigProvider>(sp =>
    sp.GetRequiredService<ControlPlaneConfigProvider>());

builder.Services.AddHealthChecks();

// ── App ───────────────────────────────────────────────────────────────────
var app = builder.Build();

app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();

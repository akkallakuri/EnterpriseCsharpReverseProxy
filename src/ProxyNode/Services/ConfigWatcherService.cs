namespace EnterpriseCsharpReverseProxy.ProxyNode.Services;

/// <summary>
/// Background service that long-polls the control plane for configuration changes
/// and pushes updates into the YARP config provider.
/// </summary>
public class ConfigWatcherService : BackgroundService
{
    private readonly ControlPlaneClient _client;
    private readonly ControlPlaneConfigProvider _configProvider;
    private readonly ILogger<ConfigWatcherService> _logger;
    private readonly int _watchTimeoutSeconds;

    public ConfigWatcherService(
        ControlPlaneClient client,
        ControlPlaneConfigProvider configProvider,
        IConfiguration configuration,
        ILogger<ConfigWatcherService> logger)
    {
        _client = client;
        _configProvider = configProvider;
        _logger = logger;
        _watchTimeoutSeconds = configuration.GetValue("ProxyNode:WatchTimeoutSeconds", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial full snapshot load
        await LoadInitialSnapshotAsync(stoppingToken);

        long knownVersion = _configProvider.GetConfig() is var cfg
            ? (cfg.Routes.Count > 0 || cfg.Clusters.Count > 0 ? long.MaxValue : -1)
            : -1;

        // Long-poll loop
        while (!stoppingToken.IsCancellationRequested)
        {
            var snapshot = await _client.WatchAsync(knownVersion, _watchTimeoutSeconds, stoppingToken);
            if (snapshot is not null)
            {
                _configProvider.Update(snapshot);
                knownVersion = snapshot.Version;
            }
        }
    }

    private async Task LoadInitialSnapshotAsync(CancellationToken ct)
    {
        const int maxRetries = 10;
        for (var i = 0; i < maxRetries; i++)
        {
            var snapshot = await _client.GetSnapshotAsync(ct);
            if (snapshot is not null)
            {
                _configProvider.Update(snapshot);
                _logger.LogInformation("Initial snapshot loaded (version={Version}).", snapshot.Version);
                return;
            }

            var delay = TimeSpan.FromSeconds(Math.Pow(2, i));  // exponential back-off
            _logger.LogWarning("Failed to load initial config, retrying in {Delay}s…", delay.TotalSeconds);
            await Task.Delay(delay, ct);
        }

        _logger.LogError("Could not load initial configuration after {Retries} attempts. Starting with empty config.", maxRetries);
    }
}

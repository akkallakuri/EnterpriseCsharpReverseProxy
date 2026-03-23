namespace EnterpriseCsharpReverseProxy.ControlPlane.Services;

/// <summary>
/// Simple change notifier backed by a CancellationTokenSource.
/// Each change replaces the current CTS so long-pollers can await the next change.
/// </summary>
public class ConfigChangeNotifier : IConfigChangeNotifier
{
    private volatile CancellationTokenSource _cts = new();
    private readonly object _lock = new();

    public void NotifyChanged()
    {
        CancellationTokenSource old;
        lock (_lock)
        {
            old = _cts;
            _cts = new CancellationTokenSource();
        }
        old.Cancel();
        old.Dispose();
    }

    public CancellationToken GetChangeToken()
    {
        lock (_lock)
            return _cts.Token;
    }
}

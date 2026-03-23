namespace EnterpriseCsharpReverseProxy.ControlPlane.Services;

/// <summary>
/// Allows components to subscribe to configuration change notifications.
/// </summary>
public interface IConfigChangeNotifier
{
    /// <summary>Raises a change event so subscribers can reload config.</summary>
    void NotifyChanged();

    /// <summary>Returns a token that is cancelled whenever configuration changes.</summary>
    CancellationToken GetChangeToken();
}

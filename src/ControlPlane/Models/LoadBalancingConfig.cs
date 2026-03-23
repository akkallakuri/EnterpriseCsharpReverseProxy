namespace EnterpriseCsharpReverseProxy.ControlPlane.Models;

public enum LoadBalancingPolicy
{
    RoundRobin,
    LeastRequests,
    Random,
    PowerOfTwoChoices,
    FirstAlphabetical
}

public class LoadBalancingConfig
{
    public LoadBalancingPolicy Policy { get; set; } = LoadBalancingPolicy.RoundRobin;
}

namespace EnterpriseCsharpReverseProxy.ControlPlane.Models;

public class RouteConfig
{
    public string RouteId { get; set; } = string.Empty;
    public string ClusterId { get; set; } = string.Empty;
    public int Order { get; set; } = 0;
    public RouteMatch Match { get; set; } = new();
    public TransformConfig? Transforms { get; set; }
    public RateLimitConfig? RateLimit { get; set; }
    public AuthorizationConfig? Authorization { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class RouteMatch
{
    public List<string>? Hosts { get; set; }
    public string? Path { get; set; }
    public List<string>? Methods { get; set; }
    public List<RouteHeader>? Headers { get; set; }
    public List<RouteQueryParameter>? QueryParameters { get; set; }
}

public class RouteHeader
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Values { get; set; }
    public HeaderMatchMode Mode { get; set; } = HeaderMatchMode.ExactHeader;
    public bool IsCaseSensitive { get; set; }
}

public class RouteQueryParameter
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Values { get; set; }
    public QueryParameterMatchMode Mode { get; set; } = QueryParameterMatchMode.Exact;
    public bool IsCaseSensitive { get; set; }
}

public enum HeaderMatchMode
{
    ExactHeader,
    HeaderPrefix,
    Exists,
    Contains,
    NotContains,
    NotExists
}

public enum QueryParameterMatchMode
{
    Exact,
    Prefix,
    Exists,
    Contains,
    NotContains
}

public class TransformConfig
{
    public string? PathPrefix { get; set; }
    public string? PathRemovePrefix { get; set; }
    public string? PathPattern { get; set; }
    public List<RequestHeaderTransform> RequestHeaders { get; set; } = new();
    public List<ResponseHeaderTransform> ResponseHeaders { get; set; } = new();
}

public class RequestHeaderTransform
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool Append { get; set; }
    public bool Remove { get; set; }
}

public class ResponseHeaderTransform
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool Append { get; set; }
    public bool Remove { get; set; }
    public bool Always { get; set; }
}

public class RateLimitConfig
{
    public string PolicyName { get; set; } = string.Empty;
}

public class AuthorizationConfig
{
    public string PolicyName { get; set; } = string.Empty;
}

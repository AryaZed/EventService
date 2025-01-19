public class AppSettings
{
    public JwtSettings Jwt { get; set; }
    public RedisSettings Redis { get; set; }
    public ElasticSearchSettings ElasticSearch { get; set; }
    public RateLimitingSettings RateLimiting { get; set; }
}

public class JwtSettings
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string Key { get; set; }
}

public class RedisSettings
{
    public string ConnectionString { get; set; }
}

public class ElasticSearchSettings
{
    public string Uri { get; set; }
}

public class RateLimitingSettings
{
    public bool EnableEndpointRateLimiting { get; set; }
    public bool StackBlockedRequests { get; set; }
    public string RealIpHeader { get; set; }
    public string ClientIdHeader { get; set; }
    public int HttpStatusCode { get; set; }
    public List<RateLimitRule> GeneralRules { get; set; } = new();
}

public class RateLimitRule
{
    public string Endpoint { get; set; }
    public string Period { get; set; }
    public int Limit { get; set; }
}
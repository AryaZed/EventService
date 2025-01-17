public class AppSettings
{
    public JwtSettings Jwt { get; set; }
    public RedisSettings Redis { get; set; }
    public ElasticSearchSettings ElasticSearch { get; set; }
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
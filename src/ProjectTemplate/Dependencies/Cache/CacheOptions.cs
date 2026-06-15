namespace ProjectTemplate.Dependencies.Cache;

/// <summary>
/// Top-level cache configuration bound from the <c>Cache</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>The configuration section name used for binding.</summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Default time-to-live for cache entries, in seconds.
    /// Individual call sites may override this value.
    /// Defaults to <c>300</c> (5 minutes).
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 300;

    /// <summary>
    /// Optional Redis configuration. When <c>null</c> or when
    /// <see cref="RedisOptions.ConnectionString"/> is empty,
    /// an in-memory distributed cache is used as the L2 backing store.
    /// </summary>
    public RedisOptions? Redis { get; set; }
}

/// <summary>
/// Configuration for the Redis distributed-cache backing store.
/// Set <c>Cache:Redis:ConnectionString</c> in <c>appsettings.json</c> or via
/// environment variables / secret manager to enable Redis for distributed deployments.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// StackExchange.Redis connection string, for example
    /// <c>localhost:6379</c> or <c>my-redis.cache.windows.net:6380,ssl=true,******
    /// Leave empty or omit to fall back to in-memory caching.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Key prefix prepended to every cache key to avoid collisions when the Redis
    /// instance is shared between multiple applications.
    /// Defaults to <c>slicedcore:</c>.
    /// </summary>
    public string InstanceName { get; set; } = "slicedcore:";
}

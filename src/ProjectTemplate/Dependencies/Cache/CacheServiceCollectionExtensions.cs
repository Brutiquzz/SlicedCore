using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace ProjectTemplate.Dependencies.Cache;

/// <summary>
/// Extension methods for registering the SlicedCore caching infrastructure with the DI container.
/// </summary>
public static class CacheServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IAppCache"/> service and its backing cache stores.
    /// <para>
    /// When <c>Cache:Redis:ConnectionString</c> is set to a non-empty value in configuration,
    /// a Redis distributed cache is used as the L2 backing store.
    /// Otherwise, a distributed in-memory cache is used, which is suitable for
    /// single-instance deployments.
    /// </para>
    /// <para>
    /// Either way, an L1 in-process memory cache is always active for the fastest possible reads.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration used to bind <see cref="CacheOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddSlicedCoreCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(CacheOptions.SectionName))
            .Validate(
                o => o.DefaultTtlSeconds > 0,
                "Cache:DefaultTtlSeconds must be a positive integer.")
            .ValidateOnStart();

        var redisConnectionString = configuration
            .GetSection(CacheOptions.SectionName)
            .GetSection(nameof(CacheOptions.Redis))[nameof(RedisOptions.ConnectionString)];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            var instanceName = configuration
                .GetSection(CacheOptions.SectionName)
                .GetSection(nameof(CacheOptions.Redis))[nameof(RedisOptions.InstanceName)]
                ?? "slicedcore:";

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = instanceName;
            });
        }
        else
        {
            // Single-instance fallback: distributed in-memory cache backed by IMemoryCache.
            services.AddDistributedMemoryCache();
        }

        services.AddHybridCache();

        return services;
    }
}

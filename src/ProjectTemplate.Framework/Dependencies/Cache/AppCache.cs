using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ProjectTemplate.Dependencies.Cache;

/// <summary>
/// Hybrid-cache implementation of <see cref="IAppCache"/>.
/// Uses <see cref="HybridCache"/> as the backing store, which provides a two-level cache:
/// an L1 in-process memory cache for ultra-low latency and an L2 distributed cache
/// (Redis when configured, otherwise a distributed-memory fallback) for consistency
/// across multiple application instances.
/// </summary>
public sealed class AppCache : IAppCache
{
    private readonly HybridCache _hybridCache;
    private readonly TimeSpan _defaultTtl;

    /// <summary>
    /// Initializes a new <see cref="AppCache"/> with the provided hybrid cache and options.
    /// </summary>
    public AppCache(HybridCache hybridCache, IOptions<CacheOptions> options)
    {
        _hybridCache = hybridCache;
        _defaultTtl = TimeSpan.FromSeconds(options.Value.DefaultTtlSeconds);
    }

    /// <inheritdoc/>
    public async Task<CachePayload?> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<CachePayload?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = ttl ?? _defaultTtl
        };

        return await _hybridCache.GetOrCreateAsync(key, factory, entryOptions, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        CachePayload payload,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = ttl ?? _defaultTtl
        };

        await _hybridCache.SetAsync(payload.Key, payload, entryOptions, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => await _hybridCache.RemoveAsync(key, cancellationToken);
}

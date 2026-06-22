namespace ProjectTemplate.Dependencies.Cache;

/// <summary>
/// Unified cache abstraction that enforces the cache-aside pattern across all features.
/// Wraps the underlying cache store (in-memory or Redis-backed hybrid cache) behind a
/// single, consistent API so features never depend on a specific cache technology.
/// </summary>
/// <remarks>
/// All read and write operations use <see cref="CachePayload"/> as the stored type so that
/// every feature shares the same <c>StampedValue&lt;CachePayload&gt;</c> slot in the L1
/// in-process cache, enabling cross-feature L1 hits without leaking private entity types.
/// Register an implementation via <c>AddSlicedCoreCache</c> in <c>Program.Services.cs</c>
/// and resolve it from the infrastructure layer using <c>GetRequiredService&lt;IAppCache&gt;()</c>.
/// Configure the backing store (in-memory vs Redis) through the <c>Cache</c> section in
/// <c>appsettings.json</c>.
/// </remarks>
public interface IAppCache
{
    /// <summary>
    /// Returns the cached <see cref="CachePayload"/> for <paramref name="key"/>.
    /// On a cache miss, invokes <paramref name="factory"/> to produce the payload,
    /// stores it with the given <paramref name="ttl"/> (or the configured default), and returns it.
    /// </summary>
    /// <param name="key">The unique cache key to look up.</param>
    /// <param name="factory">
    /// An async delegate that produces a <see cref="CachePayload"/> on a cache miss.
    /// </param>
    /// <param name="ttl">
    /// Time-to-live for the entry. <c>null</c> uses the configured default TTL.
    /// </param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<CachePayload?> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<CachePayload?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores <paramref name="payload"/> in the cache under its <see cref="CachePayload.Key"/>.
    /// Use this for write-through caching: call immediately after a successful write operation
    /// so the next read hits the cache instead of the database.
    /// </summary>
    /// <param name="payload">The payload to store. Its <see cref="CachePayload.Key"/> is used as the cache key.</param>
    /// <param name="ttl">
    /// Time-to-live for the entry. <c>null</c> uses the configured default TTL.
    /// </param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task SetAsync(
        CachePayload payload,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the cache entry for <paramref name="key"/>.
    /// Use this for cache invalidation after a destructive write (update, delete) to
    /// prevent stale data from being served to subsequent reads.
    /// </summary>
    /// <param name="key">The unique cache key to evict.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

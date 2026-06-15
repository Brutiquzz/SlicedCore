namespace ProjectTemplate.Dependencies.Cache;

/// <summary>
/// Unified cache abstraction that enforces the cache-aside pattern across all features.
/// Wraps the underlying cache store (in-memory or Redis-backed hybrid cache) behind a
/// single, consistent API so features never depend on a specific cache technology.
/// </summary>
/// <remarks>
/// Register an implementation via <c>AddSlicedCoreCache</c> in <c>Program.Services.cs</c>
/// and resolve it from the infrastructure layer using <c>GetRequiredService&lt;IAppCache&gt;()</c>.
/// Configure the backing store (in-memory vs Redis) through the <c>Cache</c> section in
/// <c>appsettings.json</c>.
/// </remarks>
public interface IAppCache
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/>.
    /// On a cache miss, invokes <paramref name="factory"/> to produce the value,
    /// stores it with the given <paramref name="ttl"/> (or the configured default), and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="factory">
    /// An async delegate that produces the value when the cache does not already contain it.
    /// </param>
    /// <param name="ttl">
    /// Time-to-live for the entry. <c>null</c> uses the configured default TTL.
    /// </param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores <paramref name="value"/> in the cache under <paramref name="key"/>.
    /// Use this for write-through caching: call immediately after a successful write operation
    /// so the next read hits the cache instead of the database.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="ttl">
    /// Time-to-live for the entry. <c>null</c> uses the configured default TTL.
    /// </param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
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

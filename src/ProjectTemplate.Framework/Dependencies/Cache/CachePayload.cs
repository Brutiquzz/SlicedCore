using System.Text.Json;

namespace ProjectTemplate.Dependencies.Cache;

/// <summary>
/// An opaque cache envelope that carries a serialized value alongside its cache key.
/// </summary>
/// <remarks>
/// Features never store their private entity types directly in the cache. Instead, they
/// call <see cref="Create{T}(object,T)"/> to produce a <see cref="CachePayload"/> that is stored as a
/// single, shared type — ensuring every feature reads and writes the same
/// <c>StampedValue&lt;CachePayload&gt;</c> in the L1 in-process cache, which guarantees
/// cross-feature L1 hits when multiple features share the same cache key.
/// </remarks>
public sealed class CachePayload
{
    /// <summary>Gets the unique cache key for this entry.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Gets the serialized bytes of the cached value.</summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    /// Returns the canonical cache key for <typeparamref name="T"/> with the given
    /// <paramref name="id"/>. The key format is <c>"{TypeName}:{id}"</c>, derived from
    /// <c>typeof(T).Name</c> so the key is consistent across all features without any
    /// manual string construction.
    /// </summary>
    /// <typeparam name="T">The cached type — its simple name forms the key prefix.</typeparam>
    /// <param name="id">A value that uniquely identifies the entry within the type, e.g. an integer primary key.</param>
    public static string KeyFor<T>(object id) => $"{typeof(T).Name}:{id}";

    /// <summary>
    /// Serializes <paramref name="value"/> and returns a <see cref="CachePayload"/>
    /// whose key is derived from <typeparamref name="T"/> and <paramref name="id"/>
    /// via <see cref="KeyFor{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="id">The identifier used to build the cache key.</param>
    /// <param name="value">The value to serialize and store.</param>
    public static CachePayload Create<T>(object id, T value)
        => new() { Key = KeyFor<T>(id), Data = JsonSerializer.SerializeToUtf8Bytes(value) };

    /// <summary>
    /// Deserializes the payload back to <typeparamref name="T"/>.
    /// Returns <c>null</c> when <see cref="Data"/> is empty.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    public T? Get<T>()
        => Data.Length == 0 ? default : JsonSerializer.Deserialize<T>(Data);
}

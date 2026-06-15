namespace ProjectTemplate.Domains.Sample;

/// <summary>
/// Shared cache payload for Sample entities.
/// Defined as an <c>internal</c> class with a public parameterless constructor so that
/// <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> can serialize and
/// deserialize it via System.Text.Json for both the L1 (in-process) and L2
/// (distributed / Redis) cache tiers.
/// </summary>
/// <remarks>
/// Both <see cref="GetSample"/> and <see cref="CreateSample"/> reference this type so that
/// a write-through from <c>CreateSample</c> produces an L1 entry that a subsequent
/// <c>GetSample</c> call can serve directly, without a type-mismatch forcing a round-trip
/// through L2 deserialization.
/// </remarks>
internal sealed class SampleCacheEntry
{
    /// <summary>Initializes a new empty <see cref="SampleCacheEntry"/>.</summary>
    /// <remarks>Required for System.Text.Json deserialization.</remarks>
    public SampleCacheEntry() { }

    /// <summary>Gets or sets the entity identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the primary name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the secondary name.</summary>
    public string Name2 { get; set; } = string.Empty;

    /// <summary>
    /// Returns the canonical cache key for a Sample entity with the specified <paramref name="id"/>.
    /// Both read (GetSample) and write (CreateSample) operations use this method to ensure they
    /// operate on the same cache entry.
    /// </summary>
    /// <param name="id">The database identifier of the Sample entity.</param>
    public static string CacheKey(int id) => $"sample:{id}";
}

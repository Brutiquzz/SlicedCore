namespace ProjectTemplate.Dependencies.Attributes;

/// <summary>
/// Stable GUID-based DI keys for each architectural layer.
/// Used with keyed service registration and resolution to enforce layer boundaries.
/// </summary>
public static class ServiceKeys
{
    /// <summary>DI key for the presentation layer.</summary>
    public const string Presentation = "f2a511e6-b445-4d7d-a7a5-9c9e646c6d23";
    /// <summary>DI key for the application layer.</summary>
    public const string Application = "b5e1b4cb-2c5b-4dc5-89a4-8db7b650bf2b";
    /// <summary>DI key for the infrastructure layer.</summary>
    public const string Infrastructure = "6c14f85b-54dd-4890-892f-c3f6c517e524";
    /// <summary>DI key for the core/domain layer.</summary>
    public const string Core = "3f97ad5a-8e19-4f5f-8f3e-3f0b29d77454";

    /// <summary>Returns the DI key string for the specified <paramref name="layer"/>.</summary>
    public static string GetKey(ServiceLayer layer) => layer switch
    {
        ServiceLayer.Presentation => Presentation,
        ServiceLayer.Application => Application,
        ServiceLayer.Infrastructure => Infrastructure,
        ServiceLayer.Core => Core,
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
    };
}

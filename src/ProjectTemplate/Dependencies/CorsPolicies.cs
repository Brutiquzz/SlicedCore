namespace ProjectTemplate.Dependencies;

/// <summary>
/// Central constants for CORS policy names used throughout the application.
/// Reference these instead of magic strings to ensure consistency.
/// </summary>
public static class CorsPolicies
{
    /// <summary>
    /// The default CORS policy applied to all endpoints.
    /// Allowed origins, methods, and headers are configured in <c>appsettings.json</c> under <c>Cors</c>.
    /// In Development all origins are permitted; in production only <c>AllowedOrigins</c> are allowed.
    /// </summary>
    public const string Default = nameof(Default);
}

namespace ProjectTemplate.Dependencies;

/// <summary>
/// Central constants for authorization policy names used throughout the application.
/// Reference these instead of magic strings to ensure consistency across endpoints.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires a valid, authenticated JWT token from the configured identity provider.
    /// No specific roles or claims beyond authentication.
    /// </summary>
    /// <remarks>
    /// Endpoints opt in via <c>.RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)</c>.
    /// The policy is registered automatically in <c>Program.Services.cs</c>.
    /// </remarks>
    public const string AuthenticatedUser = nameof(AuthenticatedUser);
}

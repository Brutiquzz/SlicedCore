namespace ProjectTemplate.Dependencies;

/// <summary>
/// Central constants for rate limiting policy names used throughout the application.
/// Reference these instead of magic strings to ensure consistency.
/// </summary>
public static class RateLimitingPolicies
{
    /// <summary>
    /// Fixed-window policy for general traffic control.
    /// Apply via <c>.RequireRateLimiting(RateLimitingPolicies.Fixed)</c> or
    /// <c>[EnableRateLimiting(RateLimitingPolicies.Fixed)]</c>.
    /// Configured under <c>RateLimiting:Fixed</c> in <c>appsettings.json</c>.
    /// </summary>
    public const string Fixed = "fixed";

    /// <summary>
    /// Sliding-window policy for fair distribution under sustained load.
    /// Apply via <c>.RequireRateLimiting(RateLimitingPolicies.Sliding)</c> or
    /// <c>[EnableRateLimiting(RateLimitingPolicies.Sliding)]</c>.
    /// Configured under <c>RateLimiting:Sliding</c> in <c>appsettings.json</c>.
    /// </summary>
    public const string Sliding = "sliding";

    /// <summary>
    /// Token-bucket policy for burst-friendly APIs.
    /// Apply via <c>.RequireRateLimiting(RateLimitingPolicies.Token)</c> or
    /// <c>[EnableRateLimiting(RateLimitingPolicies.Token)]</c>.
    /// Configured under <c>RateLimiting:Token</c> in <c>appsettings.json</c>.
    /// </summary>
    public const string Token = "token";

    /// <summary>
    /// Strict fixed-window policy with low limits, suitable for authentication and login endpoints.
    /// Apply via <c>.RequireRateLimiting(RateLimitingPolicies.Strict)</c> or
    /// <c>[EnableRateLimiting(RateLimitingPolicies.Strict)]</c>.
    /// Configured under <c>RateLimiting:Strict</c> in <c>appsettings.json</c>.
    /// </summary>
    public const string Strict = "strict";

    /// <summary>
    /// Disables rate limiting for the endpoint.
    /// Use for internal or system endpoints that must never be throttled.
    /// Apply via <c>.DisableRateLimiting()</c> or
    /// <c>[DisableRateLimiting]</c>.
    /// </summary>
    public const string Unlimited = "unlimited";
}

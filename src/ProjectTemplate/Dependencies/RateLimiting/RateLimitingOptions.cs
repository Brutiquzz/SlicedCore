namespace ProjectTemplate.Dependencies.RateLimiting;

/// <summary>
/// Top-level rate limiting configuration bound from the <c>RateLimiting</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>The configuration section name used for binding.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Global limiter settings applied to every request as a baseline.</summary>
    public GlobalLimiterOptions Global { get; set; } = new();

    /// <summary>Settings for the <c>fixed</c> named policy (default traffic control).</summary>
    public FixedWindowPolicyOptions Fixed { get; set; } = new();

    /// <summary>Settings for the <c>sliding</c> named policy (fair distribution under load).</summary>
    public SlidingWindowPolicyOptions Sliding { get; set; } = new();

    /// <summary>Settings for the <c>token</c> named policy (burst-friendly APIs).</summary>
    public TokenBucketPolicyOptions Token { get; set; } = new();

    /// <summary>Settings for the <c>strict</c> named policy (auth/login protection).</summary>
    public FixedWindowPolicyOptions Strict { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 0
    };
}

/// <summary>
/// Configuration for the global partitioned rate limiter that applies a baseline limit to every request.
/// Requests are partitioned by authenticated user identity, falling back to remote IP address, and then
/// to a shared <c>anonymous</c> bucket.
/// </summary>
public sealed class GlobalLimiterOptions
{
    /// <summary>Maximum number of permits allowed within <see cref="WindowSeconds"/>.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Size of the fixed window in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of requests that can be queued while waiting for a permit.
    /// Set to <c>0</c> to disable queuing and reject immediately when the limit is reached.
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}

/// <summary>
/// Configuration for a fixed-window rate limiting policy.
/// </summary>
public sealed class FixedWindowPolicyOptions
{
    /// <summary>Maximum number of permits allowed within <see cref="WindowSeconds"/>.</summary>
    public int PermitLimit { get; set; } = 60;

    /// <summary>Size of the fixed window in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of requests that can be queued while waiting for a permit.
    /// Set to <c>0</c> to disable queuing and reject immediately when the limit is reached.
    /// </summary>
    public int QueueLimit { get; set; } = 5;
}

/// <summary>
/// Configuration for a sliding-window rate limiting policy.
/// </summary>
public sealed class SlidingWindowPolicyOptions
{
    /// <summary>Maximum number of permits allowed within the sliding window.</summary>
    public int PermitLimit { get; set; } = 60;

    /// <summary>Total window duration in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Number of segments the window is divided into for sliding granularity.</summary>
    public int SegmentsPerWindow { get; set; } = 6;

    /// <summary>
    /// Maximum number of requests that can be queued while waiting for a permit.
    /// Set to <c>0</c> to disable queuing and reject immediately when the limit is reached.
    /// </summary>
    public int QueueLimit { get; set; } = 5;
}

/// <summary>
/// Configuration for a token-bucket rate limiting policy.
/// </summary>
public sealed class TokenBucketPolicyOptions
{
    /// <summary>Maximum number of tokens that can accumulate in the bucket.</summary>
    public int TokenLimit { get; set; } = 100;

    /// <summary>Number of tokens added to the bucket per replenishment period.</summary>
    public int TokensPerPeriod { get; set; } = 20;

    /// <summary>Replenishment period in seconds.</summary>
    public int ReplenishmentPeriodSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests that can be queued while waiting for tokens.
    /// Set to <c>0</c> to disable queuing and reject immediately when the bucket is empty.
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}

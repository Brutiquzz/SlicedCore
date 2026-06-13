namespace ProjectTemplate.Dependencies.Resilience;

/// <summary>
/// Top-level resilience configuration bound from the <c>Resilience</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>The configuration section name used for binding.</summary>
    public const string SectionName = "Resilience";

    /// <summary>Retry policy settings.</summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>Circuit breaker policy settings.</summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>Timeout policy settings.</summary>
    public TimeoutOptions Timeout { get; set; } = new();
}

/// <summary>
/// Configuration options for the retry resilience strategy.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Maximum number of retry attempts before giving up.</summary>
    public int MaxRetryAttempts { get; set; } = 4;

    /// <summary>Base delay in seconds between retry attempts.</summary>
    public int DelaySeconds { get; set; } = 2;

    /// <summary>Whether to add random jitter to the retry delay to avoid thundering herds.</summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Backoff type: <c>Exponential</c> (default), <c>Linear</c>, or <c>Constant</c>.
    /// </summary>
    public string BackoffType { get; set; } = "Exponential";
}

/// <summary>
/// Configuration options for the circuit breaker resilience strategy.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>Failure ratio (0–1) that triggers the circuit breaker.</summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>Duration in seconds of the sampling window used to calculate the failure ratio.</summary>
    public int SamplingDurationSeconds { get; set; } = 30;

    /// <summary>Minimum number of requests in the sampling window required before the circuit can trip.</summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>Duration in seconds the circuit stays open before allowing a trial request.</summary>
    public int BreakDurationSeconds { get; set; } = 15;
}

/// <summary>
/// Configuration options for the timeout resilience strategy.
/// </summary>
public sealed class TimeoutOptions
{
    /// <summary>Maximum duration in seconds allowed for a single attempt.</summary>
    public int AttemptTimeoutSeconds { get; set; } = 10;
}

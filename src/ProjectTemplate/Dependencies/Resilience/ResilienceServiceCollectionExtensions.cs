using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace ProjectTemplate.Dependencies.Resilience;

/// <summary>
/// Extension methods for registering configuration-driven resilience policies with the DI container.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <c>Default</c> named <see cref="HttpClient"/> with a resilience pipeline
    /// (retry, circuit breaker, timeout) driven by the <c>Resilience</c> section of
    /// <c>appsettings.json</c>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration used to bind <see cref="ResilienceOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddSlicedCoreResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResilienceOptions>(
            configuration.GetSection(ResilienceOptions.SectionName));

        services.AddHttpClient("Default")
            .AddResilienceHandler("slicedcore-resilience", (builder, context) =>
            {
                var options = context.ServiceProvider
                    .GetRequiredService<IOptions<ResilienceOptions>>()
                    .Value;

                builder
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.Retry.MaxRetryAttempts,
                        Delay = TimeSpan.FromSeconds(options.Retry.DelaySeconds),
                        BackoffType = ParseBackoffType(options.Retry.BackoffType),
                        UseJitter = options.Retry.UseJitter,
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .HandleResult(response => !response.IsSuccessStatusCode)
                    })
                    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = options.CircuitBreaker.FailureRatio,
                        SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                        MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                        BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds)
                    })
                    .AddTimeout(TimeSpan.FromSeconds(options.Timeout.AttemptTimeoutSeconds));
            });

        return services;
    }

    private static DelayBackoffType ParseBackoffType(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "constant" => DelayBackoffType.Constant,
            "linear" => DelayBackoffType.Linear,
            _ => DelayBackoffType.Exponential
        };
}

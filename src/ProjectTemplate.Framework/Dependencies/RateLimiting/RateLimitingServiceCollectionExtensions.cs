using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectTemplate.Dependencies;
using System.Net;
using System.Threading.RateLimiting;

namespace ProjectTemplate.Dependencies.RateLimiting;

/// <summary>
/// Extension methods for registering configuration-driven rate limiting policies with the DI container.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the global partitioned rate limiter and the standard named policies
    /// (<c>fixed</c>, <c>sliding</c>, <c>token</c>, <c>strict</c>, <c>unlimited</c>)
    /// driven by the <c>RateLimiting</c> section of <c>appsettings.json</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The global limiter partitions traffic by authenticated user identity, falling back to
    /// the remote IP address, and finally to the shared key <c>anonymous</c>.
    /// </para>
    /// <para>
    /// Named policies can be applied per-endpoint with
    /// <c>.RequireRateLimiting("policy-name")</c> or
    /// <c>[EnableRateLimiting("policy-name")]</c>.
    /// Use <c>.DisableRateLimiting()</c> or <c>[DisableRateLimiting]</c> to opt out
    /// for internal endpoints.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration used to bind <see cref="RateLimitingOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddSlicedCoreRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .Validate(o =>
                o.Global.PermitLimit > 0 &&
                o.Global.WindowSeconds > 0 &&
                o.Global.QueueLimit >= 0 &&
                o.Fixed.PermitLimit > 0 &&
                o.Fixed.WindowSeconds > 0 &&
                o.Fixed.QueueLimit >= 0 &&
                o.Sliding.PermitLimit > 0 &&
                o.Sliding.WindowSeconds > 0 &&
                o.Sliding.SegmentsPerWindow > 0 &&
                o.Sliding.QueueLimit >= 0 &&
                o.Token.TokenLimit > 0 &&
                o.Token.TokensPerPeriod > 0 &&
                o.Token.ReplenishmentPeriodSeconds > 0 &&
                o.Token.QueueLimit >= 0 &&
                o.Strict.PermitLimit > 0 &&
                o.Strict.WindowSeconds > 0 &&
                o.Strict.QueueLimit >= 0,
                "Invalid RateLimiting configuration values.")
            .ValidateOnStart();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.OnRejected = (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimitingServiceCollectionExtensions));

                var partitionKey = context.HttpContext.User.Identity?.Name
                    ?? context.HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                var endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? context.HttpContext.Request.Path.ToString();
                var sanitizedEndpoint = SanitizeLogValue(endpoint);
                var policy = context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reason) ? reason : "global";
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? (TimeSpan?)retryAfterValue
                    : null;

                logger.LogWarning(
                    "Rate limit exceeded. Partition={PartitionKey} Endpoint={Endpoint} Policy={Policy} RetryAfter={RetryAfter}",
                    partitionKey,
                    sanitizedEndpoint,
                    policy,
                    retryAfter?.TotalSeconds is { } seconds ? $"{seconds}s" : "N/A");

                if (retryAfter.HasValue)
                {
                    context.HttpContext.Response.Headers[HttpResponseHeader.RetryAfter.ToString()] =
                        ((int)retryAfter.Value.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            // Global limiter: applied to every request before named policies.
            // Partitioned by authenticated user identity → remote IP → "anonymous".
            limiterOptions.GlobalLimiter =
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var options = context.RequestServices
                        .GetRequiredService<IOptions<RateLimitingOptions>>()
                        .Value
                        .Global;

                    var partitionKey = context.User.Identity?.Name
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.PermitLimit,
                            Window = TimeSpan.FromSeconds(options.WindowSeconds),
                            QueueLimit = options.QueueLimit,
                            AutoReplenishment = true
                        });
                });

            // Named policies — resolved lazily so options changes are respected at startup.
            limiterOptions.AddPolicy(RateLimitingPolicies.Fixed, context =>
            {
                var opts = context.RequestServices
                    .GetRequiredService<IOptions<RateLimitingOptions>>()
                    .Value
                    .Fixed;

                return RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.WindowSeconds),
                        QueueLimit = opts.QueueLimit,
                        AutoReplenishment = true
                    });
            });

            limiterOptions.AddPolicy(RateLimitingPolicies.Sliding, context =>
            {
                var opts = context.RequestServices
                    .GetRequiredService<IOptions<RateLimitingOptions>>()
                    .Value
                    .Sliding;

                return RateLimitPartition.GetSlidingWindowLimiter(
                    GetPartitionKey(context),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = opts.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.WindowSeconds),
                        SegmentsPerWindow = opts.SegmentsPerWindow,
                        QueueLimit = opts.QueueLimit,
                        AutoReplenishment = true
                    });
            });

            limiterOptions.AddPolicy(RateLimitingPolicies.Token, context =>
            {
                var opts = context.RequestServices
                    .GetRequiredService<IOptions<RateLimitingOptions>>()
                    .Value
                    .Token;

                return RateLimitPartition.GetTokenBucketLimiter(
                    GetPartitionKey(context),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = opts.TokenLimit,
                        TokensPerPeriod = opts.TokensPerPeriod,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(opts.ReplenishmentPeriodSeconds),
                        QueueLimit = opts.QueueLimit,
                        AutoReplenishment = true
                    });
            });

            limiterOptions.AddPolicy(RateLimitingPolicies.Strict, context =>
            {
                var opts = context.RequestServices
                    .GetRequiredService<IOptions<RateLimitingOptions>>()
                    .Value
                    .Strict;

                return RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.PermitLimit,
                        Window = TimeSpan.FromSeconds(opts.WindowSeconds),
                        QueueLimit = opts.QueueLimit,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context) =>
        context.User.Identity?.Name
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";

    /// <summary>
    /// Strips newline and carriage-return characters from a user-provided string to prevent log forging.
    /// </summary>
    private static string SanitizeLogValue(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
             .Replace("\n", string.Empty, StringComparison.Ordinal);
}

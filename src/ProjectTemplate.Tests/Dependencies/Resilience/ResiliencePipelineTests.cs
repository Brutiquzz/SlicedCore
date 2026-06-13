using Microsoft.Extensions.Configuration;
using ProjectTemplate.Dependencies.Resilience;

namespace ProjectTemplate.Tests.Dependencies.Resilience;

public sealed class ResiliencePipelineTests
{
    [Test]
    public async Task AddSlicedCoreResilience_RegistersHttpClientFactory()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Retry:MaxRetryAttempts"] = "3",
                ["Resilience:Retry:DelaySeconds"] = "1",
                ["Resilience:Retry:UseJitter"] = "true",
                ["Resilience:Retry:BackoffType"] = "Exponential",
                ["Resilience:CircuitBreaker:FailureRatio"] = "0.5",
                ["Resilience:CircuitBreaker:SamplingDurationSeconds"] = "30",
                ["Resilience:CircuitBreaker:MinimumThroughput"] = "10",
                ["Resilience:CircuitBreaker:BreakDurationSeconds"] = "15",
                ["Resilience:Timeout:AttemptTimeoutSeconds"] = "10"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreResilience(config);
        var provider = services.BuildServiceProvider();

        var factory = provider.GetService<IHttpClientFactory>();

        await Assert.That(factory).IsNotNull();
    }

    [Test]
    public async Task AddSlicedCoreResilience_BindsOptionsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Retry:MaxRetryAttempts"] = "5",
                ["Resilience:Retry:DelaySeconds"] = "3",
                ["Resilience:Retry:UseJitter"] = "false",
                ["Resilience:Retry:BackoffType"] = "Linear",
                ["Resilience:CircuitBreaker:FailureRatio"] = "0.7",
                ["Resilience:CircuitBreaker:SamplingDurationSeconds"] = "60",
                ["Resilience:CircuitBreaker:MinimumThroughput"] = "20",
                ["Resilience:CircuitBreaker:BreakDurationSeconds"] = "30",
                ["Resilience:Timeout:AttemptTimeoutSeconds"] = "5"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreResilience(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResilienceOptions>>().Value;

        await Assert.That(options.Retry.MaxRetryAttempts).IsEqualTo(5);
        await Assert.That(options.Retry.DelaySeconds).IsEqualTo(3);
        await Assert.That(options.Retry.UseJitter).IsFalse();
        await Assert.That(options.Retry.BackoffType).IsEqualTo("Linear");
        await Assert.That(options.CircuitBreaker.FailureRatio).IsEqualTo(0.7);
        await Assert.That(options.CircuitBreaker.SamplingDurationSeconds).IsEqualTo(60);
        await Assert.That(options.CircuitBreaker.MinimumThroughput).IsEqualTo(20);
        await Assert.That(options.CircuitBreaker.BreakDurationSeconds).IsEqualTo(30);
        await Assert.That(options.Timeout.AttemptTimeoutSeconds).IsEqualTo(5);
    }

    [Test]
    public async Task AddSlicedCoreResilience_UsesDefaultValues_WhenConfigurationSectionIsAbsent()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreResilience(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResilienceOptions>>().Value;

        await Assert.That(options.Retry.MaxRetryAttempts).IsEqualTo(4);
        await Assert.That(options.Retry.DelaySeconds).IsEqualTo(2);
        await Assert.That(options.Retry.UseJitter).IsTrue();
        await Assert.That(options.Retry.BackoffType).IsEqualTo("Exponential");
        await Assert.That(options.CircuitBreaker.FailureRatio).IsEqualTo(0.5);
        await Assert.That(options.CircuitBreaker.SamplingDurationSeconds).IsEqualTo(30);
        await Assert.That(options.CircuitBreaker.MinimumThroughput).IsEqualTo(10);
        await Assert.That(options.CircuitBreaker.BreakDurationSeconds).IsEqualTo(15);
        await Assert.That(options.Timeout.AttemptTimeoutSeconds).IsEqualTo(10);
    }
}

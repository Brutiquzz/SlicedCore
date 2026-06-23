using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ProjectTemplate.Dependencies.RateLimiting;

namespace ProjectTemplate.Tests.Dependencies.RateLimiting;

public sealed class RateLimitingServiceTests
{
    [Test]
    public async Task AddSlicedCoreRateLimiting_RegistersRateLimiterServices()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreRateLimiting(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetService<IOptions<RateLimitingOptions>>();

        await Assert.That(options).IsNotNull();
    }

    [Test]
    public async Task AddSlicedCoreRateLimiting_BindsGlobalOptionsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = "200",
                ["RateLimiting:Global:WindowSeconds"] = "30",
                ["RateLimiting:Global:QueueLimit"] = "20"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreRateLimiting(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        await Assert.That(options.Global.PermitLimit).IsEqualTo(200);
        await Assert.That(options.Global.WindowSeconds).IsEqualTo(30);
        await Assert.That(options.Global.QueueLimit).IsEqualTo(20);
    }

    [Test]
    public async Task AddSlicedCoreRateLimiting_BindsFixedPolicyOptionsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Fixed:PermitLimit"] = "120",
                ["RateLimiting:Fixed:WindowSeconds"] = "90",
                ["RateLimiting:Fixed:QueueLimit"] = "8"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreRateLimiting(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        await Assert.That(options.Fixed.PermitLimit).IsEqualTo(120);
        await Assert.That(options.Fixed.WindowSeconds).IsEqualTo(90);
        await Assert.That(options.Fixed.QueueLimit).IsEqualTo(8);
    }

    [Test]
    public async Task AddSlicedCoreRateLimiting_BindsSlidingPolicyOptionsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Sliding:PermitLimit"] = "80",
                ["RateLimiting:Sliding:WindowSeconds"] = "45",
                ["RateLimiting:Sliding:SegmentsPerWindow"] = "9",
                ["RateLimiting:Sliding:QueueLimit"] = "3"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreRateLimiting(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        await Assert.That(options.Sliding.PermitLimit).IsEqualTo(80);
        await Assert.That(options.Sliding.WindowSeconds).IsEqualTo(45);
        await Assert.That(options.Sliding.SegmentsPerWindow).IsEqualTo(9);
        await Assert.That(options.Sliding.QueueLimit).IsEqualTo(3);
    }

    [Test]
    public async Task AddSlicedCoreRateLimiting_BindsTokenPolicyOptionsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Token:TokenLimit"] = "50",
                ["RateLimiting:Token:TokensPerPeriod"] = "10",
                ["RateLimiting:Token:ReplenishmentPeriodSeconds"] = "5",
                ["RateLimiting:Token:QueueLimit"] = "15"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreRateLimiting(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        await Assert.That(options.Token.TokenLimit).IsEqualTo(50);
        await Assert.That(options.Token.TokensPerPeriod).IsEqualTo(10);
        await Assert.That(options.Token.ReplenishmentPeriodSeconds).IsEqualTo(5);
        await Assert.That(options.Token.QueueLimit).IsEqualTo(15);
    }

    [Test]
    public async Task AddSlicedCoreRateLimiting_BindsStrictPolicyOptionsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Strict:PermitLimit"] = "5",
                ["RateLimiting:Strict:WindowSeconds"] = "120",
                ["RateLimiting:Strict:QueueLimit"] = "0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreRateLimiting(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        await Assert.That(options.Strict.PermitLimit).IsEqualTo(5);
        await Assert.That(options.Strict.WindowSeconds).IsEqualTo(120);
        await Assert.That(options.Strict.QueueLimit).IsEqualTo(0);
    }

    [Test]
    public async Task AddSlicedCoreRateLimiting_UsesDefaultValues_WhenConfigurationSectionIsAbsent()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreRateLimiting(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        await Assert.That(options.Global.PermitLimit).IsEqualTo(100);
        await Assert.That(options.Global.WindowSeconds).IsEqualTo(60);
        await Assert.That(options.Global.QueueLimit).IsEqualTo(10);

        await Assert.That(options.Fixed.PermitLimit).IsEqualTo(60);
        await Assert.That(options.Fixed.WindowSeconds).IsEqualTo(60);
        await Assert.That(options.Fixed.QueueLimit).IsEqualTo(5);

        await Assert.That(options.Sliding.PermitLimit).IsEqualTo(60);
        await Assert.That(options.Sliding.WindowSeconds).IsEqualTo(60);
        await Assert.That(options.Sliding.SegmentsPerWindow).IsEqualTo(6);
        await Assert.That(options.Sliding.QueueLimit).IsEqualTo(5);

        await Assert.That(options.Token.TokenLimit).IsEqualTo(100);
        await Assert.That(options.Token.TokensPerPeriod).IsEqualTo(20);
        await Assert.That(options.Token.ReplenishmentPeriodSeconds).IsEqualTo(10);
        await Assert.That(options.Token.QueueLimit).IsEqualTo(10);

        await Assert.That(options.Strict.PermitLimit).IsEqualTo(10);
        await Assert.That(options.Strict.WindowSeconds).IsEqualTo(60);
        await Assert.That(options.Strict.QueueLimit).IsEqualTo(0);
    }
}

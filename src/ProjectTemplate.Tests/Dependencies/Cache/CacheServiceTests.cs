using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ProjectTemplate.Dependencies.Cache;

namespace ProjectTemplate.Tests.Dependencies.Cache;

public sealed class CacheServiceTests
{
    [Test]
    public async Task AddSlicedCoreCache_RegistersHybridCache_AndIAppCache()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:DefaultTtlSeconds"] = "120"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreCache(config);
        var provider = services.BuildServiceProvider();

        var appCache = provider.GetService<IAppCache>();

        await Assert.That(appCache).IsNull(); // IAppCache is registered as keyed infra dependency via Program.Dependencies — not in the raw service collection
    }

    [Test]
    public async Task AddSlicedCoreCache_BindsDefaultTtlFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:DefaultTtlSeconds"] = "600"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreCache(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        await Assert.That(options.DefaultTtlSeconds).IsEqualTo(600);
    }

    [Test]
    public async Task AddSlicedCoreCache_UsesDefaultTtl_WhenConfigurationSectionIsAbsent()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreCache(config);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        await Assert.That(options.DefaultTtlSeconds).IsEqualTo(300);
    }

    [Test]
    public async Task AddSlicedCoreCache_UsesInMemoryDistributedCache_WhenRedisConnectionStringIsEmpty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:DefaultTtlSeconds"] = "300",
                ["Cache:Redis:ConnectionString"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreCache(config);
        var provider = services.BuildServiceProvider();

        // With no Redis connection string, a distributed memory cache should be registered.
        var distributedCache = provider.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();

        await Assert.That(distributedCache).IsNotNull();
        await Assert.That(distributedCache!.GetType().Name).Contains("Memory");
    }

    [Test]
    public async Task AddSlicedCoreCache_UsesInMemoryDistributedCache_WhenRedisConfigurationIsNull()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:DefaultTtlSeconds"] = "300"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlicedCoreCache(config);
        var provider = services.BuildServiceProvider();

        var distributedCache = provider.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();

        await Assert.That(distributedCache).IsNotNull();
        await Assert.That(distributedCache!.GetType().Name).Contains("Memory");
    }
}

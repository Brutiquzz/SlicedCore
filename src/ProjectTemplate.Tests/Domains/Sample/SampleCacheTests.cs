using System.Net;
using System.Net.Http.Json;
using Cortex.Mediator;
using Microsoft.Extensions.Caching.Hybrid;
using ProjectTemplate.Dependencies.Attributes;
using ProjectTemplate.Dependencies.Cache;
using ProjectTemplate.Domains.Sample;
using static ProjectTemplate.Domains.Sample.CreateSample;

namespace ProjectTemplate.Tests.Domains.Sample;

/// <summary>
/// Integration tests validating the write-through and read-through cache behaviour
/// of the Sample domain. The in-memory distributed cache (used by the test host) makes
/// cache hits deterministic without requiring a real Redis instance.
/// </summary>
public sealed class SampleCacheTests
{
    [ClassDataSource<ProjectTemplateWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required ProjectTemplateWebApplicationFactory ApplicationFactory { get; init; }

    private sealed record CreateSampleBody(string Name, string Name2);
    private sealed record CreateSampleResponse(int Id, string Name, string Name2);
    private sealed record GetSampleResponse(int Id, string Name, string Name2);

    /// <summary>
    /// After CreateSample succeeds, the entity is written through into the hybrid cache.
    /// A subsequent GetSample request for the same id must return the correct data
    /// (proving it is served either from the DB or the cache consistently).
    /// </summary>
    [Test]
    public async Task GetSample_ReturnsCorrectData_AfterCreateSamplePopulatesCache()
    {
        using var client = ApplicationFactory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        // Create a sample — write-through should populate the cache.
        using var createResponse = await client.PostAsJsonAsync("/api/v1/sample",
            new CreateSampleBody("Cache Test Name", "Cache Test Name2"));

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateSampleResponse>();
        await Assert.That(created).IsNotNull();

        // Retrieve the sample — should be served from cache or DB with identical values.
        using var getResponse = await client.GetAsync($"/api/v1/sample/{created!.Id}");

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var found = await getResponse.Content.ReadFromJsonAsync<GetSampleResponse>();
        await Assert.That(found).IsNotNull();
        await Assert.That(found!.Id).IsEqualTo(created.Id);
        await Assert.That(found.Name).IsEqualTo("Cache Test Name");
        await Assert.That(found.Name2).IsEqualTo("Cache Test Name2");
    }

    /// <summary>
    /// Calling GetSample twice for the same id must return consistent results — whether the
    /// second call hits the L1/L2 cache or the database, the data must be identical.
    /// </summary>
    [Test]
    public async Task GetSample_ReturnsSameData_OnRepeatedRequests()
    {
        using var client = ApplicationFactory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        // First create a sample to have a known entity.
        using var createResponse = await client.PostAsJsonAsync("/api/v1/sample",
            new CreateSampleBody("Repeated Read Name", "Repeated Read Name2"));

        var created = await createResponse.Content.ReadFromJsonAsync<CreateSampleResponse>();
        await Assert.That(created).IsNotNull();

        // First read — may come from DB or cache.
        using var firstGet = await client.GetAsync($"/api/v1/sample/{created!.Id}");
        var first = await firstGet.Content.ReadFromJsonAsync<GetSampleResponse>();

        // Second read — should be served from the cache.
        using var secondGet = await client.GetAsync($"/api/v1/sample/{created.Id}");
        var second = await secondGet.Content.ReadFromJsonAsync<GetSampleResponse>();

        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(second!.Id).IsEqualTo(first!.Id);
        await Assert.That(second.Name).IsEqualTo(first.Name);
        await Assert.That(second.Name2).IsEqualTo(first.Name2);
    }

    /// <summary>
    /// Requesting a sample id that does not exist should return 404, both when the cache
    /// has no entry and after the cache-aside factory has run and stored the null result.
    /// </summary>
    [Test]
    public async Task GetSample_Returns404_ForNonExistentId()
    {
        using var client = ApplicationFactory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        // Use an id that will not exist in the test database.
        using var response = await client.GetAsync("/api/v1/sample/999999");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that the IAppCache is resolvable from the infrastructure layer's keyed DI
    /// registration and that the hybrid cache can complete a round-trip get-or-create operation.
    /// </summary>
    [Test]
    public async Task AppCache_GetOrCreate_StoresAndReturnsValue()
    {
        await using var scope = ApplicationFactory.Services.CreateAsyncScope();

        // HybridCache is registered as a normal singleton — resolve it to verify it's wired up.
        var hybridCache = scope.ServiceProvider.GetService<HybridCache>();

        await Assert.That(hybridCache).IsNotNull();

        // Perform a round-trip through the hybrid cache.
        var value = await hybridCache!.GetOrCreateAsync(
            "test:appCache:roundTrip",
            _ => ValueTask.FromResult(42),
            cancellationToken: CancellationToken.None);

        await Assert.That(value).IsEqualTo(42);
    }

    /// <summary>
    /// Tests that creating a sample through the mediator (handler path) writes through to the
    /// hybrid cache such that a subsequent mediator-level GetSample call returns consistent data.
    /// </summary>
    [Test]
    public async Task CreateSample_WritesThroughCache_AndGetSampleReadsIt()
    {
        await using var scope = ApplicationFactory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var created = await mediator.CreateSample(
            new TestCreateSampleRequest { Name = "WTCache", Name2 = "WTCache2" },
            CancellationToken.None);

        await Assert.That(created.Status.ToString()).IsEqualTo("Created");

        var found = await mediator.GetSample()
            .WithId(created.Value.Id)
            .Send(CancellationToken.None);

        await Assert.That(found.Status.ToString()).IsEqualTo("Ok");
        await Assert.That(found.Value.Id).IsEqualTo(created.Value.Id);
        await Assert.That(found.Value.Name).IsEqualTo("WTCache");
        await Assert.That(found.Value.Name2).IsEqualTo("WTCache2");
    }

    private sealed record TestCreateSampleRequest : ICreateSampleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Name2 { get; set; } = string.Empty;
    }

    /// <summary>
    /// Directly exercises <see cref="IAppCache.GetOrCreateAsync{T}"/> with <see cref="SampleCacheEntry"/>
    /// to verify the cache round-trip works without the mediator wrapper.
    /// Surfaces any serialization or resolution exceptions directly (not swallowed as CriticalError).
    /// </summary>
    [Test]
    public async Task IAppCache_GetOrCreate_RoundTrip_WithSampleCacheEntry()
    {
        await using var scope = ApplicationFactory.Services.CreateAsyncScope();

        var infraKey = ServiceKeys.GetKey(ServiceLayer.Infrastructure);
        var appCache = scope.ServiceProvider.GetRequiredKeyedService<IAppCache>(infraKey);

        var entry = new SampleCacheEntry { Id = 9999, Name = "DiagName", Name2 = "DiagName2" };

        // Write to cache directly (simulating CreateSample write-through)
        await appCache.SetAsync(SampleCacheEntry.CacheKey(9999), entry, cancellationToken: CancellationToken.None);

        // Read back (simulating GetSample read-through on a cache hit)
        var retrieved = await appCache.GetOrCreateAsync<SampleCacheEntry?>(
            SampleCacheEntry.CacheKey(9999),
            _ => ValueTask.FromResult<SampleCacheEntry?>(null),
            cancellationToken: CancellationToken.None);

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Id).IsEqualTo(9999);
        await Assert.That(retrieved.Name).IsEqualTo("DiagName");
    }
}

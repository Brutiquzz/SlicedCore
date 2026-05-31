using System.Net;
using System.Net.Http.Json;

namespace ProjectTemplate.Tests.Domains.Sample;

public partial class CreateSampleTests
{
    private sealed record CreateSampleResponse(int Id, string Name, string Name2);

    [Test]
    public async Task CreateSampleEndpoint_ReturnsCreatedResponse()
    {
        using var client = ApplicationFactory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync("/api/v1/sample", new
        {
            Name = "Sample Name",
            Name2 = "Sample Name2"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateSampleResponse>();

        await Assert.That(body is not null).IsTrue();

        if (body is null)
        {
            throw new InvalidOperationException("Expected a JSON response body.");
        }

        await Assert.That(body.Id).IsGreaterThan(0);
        await Assert.That(body.Name).IsEqualTo("Sample Name");
        await Assert.That(body.Name2).IsEqualTo("Sample Name2");
    }
}

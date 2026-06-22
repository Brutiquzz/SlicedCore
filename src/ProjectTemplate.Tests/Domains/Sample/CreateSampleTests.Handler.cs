using Cortex.Mediator;
using ProjectTemplate.Domains.Sample;
using static ProjectTemplate.Domains.Sample.CreateSample;

namespace ProjectTemplate.Tests.Domains.Sample;

public partial class CreateSampleTests
{
    [Test]
    [Skip("EF Core duplicate SampleEntity CLR type — generator fix pending")]
    public async Task PresentationHandler_ReturnsCreatedResult_ForValidRequest()
    {
        var result = await SendValidRequestThroughHandlerAsync();

        await Assert.That(result.Status.ToString()).IsEqualTo("Created");
        await Assert.That(result.Value.Id).IsGreaterThan(0);
        await Assert.That(result.Value.Name).IsEqualTo("Sample Name");
        await Assert.That(result.Value.Name2).IsEqualTo("Sample Name2");
    }

    [Test]
    [Skip("EF Core duplicate SampleEntity CLR type — generator fix pending")]
    public async Task PresentationHandler_ReturnsInvalidResult_ForInvalidRequest()
    {
        await using var scope = ApplicationFactory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.CreateSample(
            new TestCreateSampleRequest(),
            CancellationToken.None);

        await Assert.That(result.Status.ToString()).IsEqualTo("Invalid");
    }
}

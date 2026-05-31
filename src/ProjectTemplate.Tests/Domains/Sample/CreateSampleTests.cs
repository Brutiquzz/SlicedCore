using Cortex.Mediator;
using ProjectTemplate.Domains.Sample;
using static ProjectTemplate.Domains.Sample.CreateSample;

namespace ProjectTemplate.Tests.Domains.Sample;

public partial class CreateSampleTests
{
    [ClassDataSource<ProjectTemplateWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required ProjectTemplateWebApplicationFactory ApplicationFactory { get; init; }

    private sealed record TestCreateSampleRequest : ICreateSampleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Name2 { get; set; } = string.Empty;
    }

    private async Task<Result<ICreateSampleResponse>> SendValidRequestThroughHandlerAsync()
    {
        await using var scope = ApplicationFactory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.CreateSample(
            new TestCreateSampleRequest
            {
                Name = "Sample Name",
                Name2 = "Sample Name2"
            },
            CancellationToken.None);
    }
}

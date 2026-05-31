namespace ProjectTemplate.Domains.Sample;

public partial class CreateSample
{
    partial class PresentationLayer
    {
        private sealed class CreateSampleFeatureEndpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder builder)
            {
                builder.MapPost("/sample", async (ICreateSampleRequest request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    return (await mediator.CreateSample(request, cancellationToken)).ToMinimalApiResult();
                })
                    .WithName("CreateSample")
                    .WithSummary("Creating a Sample")
                    .WithDescription("This is a description on how a sample gets created")
                    .Produces<ICreateSampleResponse>(StatusCodes.Status201Created)
                    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                    .Accepts<ICreateSampleRequest>("application/json")
                    // Authorization: endpoints default to anonymous access for maximum flexibility.
                    // To require JWT authentication, replace .AllowAnonymous() with:
                    //     .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
                    // Ensure your appsettings.json JwtBearer section is configured with your IdP's Authority and Audience.
                    .AllowAnonymous()
                    .HasApiVersion(1, 0)
                    .Stable() // .Experimental() // .Deprecated() // .Hidden()
                    .WithTags("Sample", "Create");
            }
        }
    }
}

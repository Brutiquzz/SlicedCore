namespace ProjectTemplate.Domains.Sample;

public partial class CreateSample
{
    partial class PresentationLayer
    {
        private sealed class CreateSampleFeatureEndpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder builder)
            {
//#if (isCommand)
                builder.MapPost("/sample", async (CreateSampleRequest request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    return (await mediator.CreateSample(request, cancellationToken)).ToMinimalApiResult();
                })
                    .WithName("CreateSample")
                    .WithSummary("Creating a Sample")
                    .WithDescription("This is a description on how a sample gets created")
                    .Produces<ICreateSampleResponse>(StatusCodes.Status201Created)
                    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                    .Accepts<ICreateSampleRequest>("application/json")
//#else
                builder.MapGet("/sample/{id:int}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    return (await mediator.CreateSample().WithId(id).Send(cancellationToken)).ToMinimalApiResult();
                })
                    .WithName("GetSample")
                    .WithSummary("Getting a Sample")
                    .WithDescription("This is a description on how a sample gets retrieved")
                    .Produces<ICreateSampleResponse>(StatusCodes.Status200OK)
                    .Produces(StatusCodes.Status404NotFound)
//#endif
                    // Authorization: endpoints default to anonymous access for maximum flexibility.
                    // To require JWT authentication, replace .AllowAnonymous() with:
                    //     .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
                    // Ensure your appsettings.json JwtBearer section is configured with your IdP's Authority and Audience.
                    .AllowAnonymous()
                    .HasApiVersion(1, 0)
                    .Stable() // .Experimental() // .Deprecated() // .Hidden()
//#if (isCommand)
                    .WithTags("Sample", "Create");
//#else
                    .WithTags("Sample", "Get");
//#endif
            }
        }
    }
}

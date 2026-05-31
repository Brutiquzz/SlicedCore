namespace ProjectTemplate.Domains.Sample;

public partial class GetSample
{
    partial class PresentationLayer
    {
        private sealed class GetSampleFeatureEndpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder builder)
            {
                builder.MapGet("/sample/{id:int}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    return (await mediator.GetSample().WithId(id).Send(cancellationToken)).ToMinimalApiResult();
                })
                    .WithName("GetSample")
                    .WithSummary("Getting a Sample")
                    .WithDescription("This is a description on how a sample gets retrieved")
                    .Produces<IGetSampleResponse>(StatusCodes.Status200OK)
                    .Produces(StatusCodes.Status404NotFound)
                    // Authorization: endpoints default to anonymous access for maximum flexibility.
                    // To require JWT authentication, replace .AllowAnonymous() with:
                    //     .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
                    // Ensure your appsettings.json JwtBearer section is configured with your IdP's Authority and Audience.
                    .AllowAnonymous()
                    .HasApiVersion(1, 0)
                    .Stable() // .Experimental() // .Deprecated() // .Hidden()
                    .WithTags("Sample", "Get");
            }
        }
    }
}

namespace ProjectTemplate.Domains.Sample;

public partial class CreateSample
{
    partial class PresentationLayer
    {
        private sealed class CreateSampleFeatureEndpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder builder)
            {
//#if (isPost)
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
//#elseif (isPut)
                builder.MapPut("/sample/{id:int}", async (int id, CreateSampleRequest request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    request.Id = id;
                    return (await mediator.CreateSample(request, cancellationToken)).ToMinimalApiResult();
                })
                    .WithName("UpdateSample")
                    .WithSummary("Updating a Sample")
                    .WithDescription("This is a description on how a sample gets updated")
                    .Produces<ICreateSampleResponse>(StatusCodes.Status200OK)
                    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                    .Produces(StatusCodes.Status404NotFound)
                    .Accepts<ICreateSampleRequest>("application/json")
//#elseif (isPatch)
                builder.MapPatch("/sample/{id:int}", async (int id, CreateSampleRequest request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    request.Id = id;
                    return (await mediator.CreateSample(request, cancellationToken)).ToMinimalApiResult();
                })
                    .WithName("PatchSample")
                    .WithSummary("Patching a Sample")
                    .WithDescription("This is a description on how a sample gets partially updated")
                    .Produces<ICreateSampleResponse>(StatusCodes.Status200OK)
                    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                    .Produces(StatusCodes.Status404NotFound)
                    .Accepts<ICreateSampleRequest>("application/json")
//#elseif (isDelete)
                builder.MapDelete("/sample/{id:int}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    return (await mediator.CreateSample(new CreateSampleRequest { Id = id }, cancellationToken)).ToMinimalApiResult();
                })
                    .WithName("DeleteSample")
                    .WithSummary("Deleting a Sample")
                    .WithDescription("This is a description on how a sample gets deleted")
                    .Produces(StatusCodes.Status204NoContent)
                    .Produces(StatusCodes.Status404NotFound)
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
//#if (isPost)
                    .WithTags("Sample", "Create");
//#elseif (isPut)
                    .WithTags("Sample", "Update");
//#elseif (isPatch)
                    .WithTags("Sample", "Patch");
//#elseif (isDelete)
                    .WithTags("Sample", "Delete");
//#else
                    .WithTags("Sample", "Get");
//#endif
            }
        }
    }
}

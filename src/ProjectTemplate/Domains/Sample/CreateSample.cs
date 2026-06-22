using ProjectTemplate.Data;
using ProjectTemplate.Dependencies.Attributes;
using ProjectTemplate.Dependencies.Cache;

namespace ProjectTemplate.Domains.Sample;

[Feature(FeatureType.Command)]
public partial class CreateSample
{
    partial class PresentationLayer
    {
        private async Task<Result<ICreateSampleResponse>> PresentationLogic(ICreateSampleRequest request, CancellationToken cancellationToken)
        {
            // Implement any further Presentation Code for the CreateSampleFeature here...
            // Use GetRequired or Get to access accessable dependencies from this layer
            // ...

            // Implement presentation logic
            // ...

            var appDTOResponse = await ForwardToApplicationLayer(request.Adapt<CreateSampleRequestDTO>(), cancellationToken);

            if (appDTOResponse.IsError())
            {
                LogError(appDTOResponse.Errors);
                return Result.Error(new ErrorList(appDTOResponse.Errors));
            }

            return Result.Created((ICreateSampleResponse)appDTOResponse.Value.Adapt<CreateSampleResponse>());
        }

        public sealed class CreateSampleRequestValidator : AbstractValidator<ICreateSampleRequest>
        {
            public CreateSampleRequestValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Name is required")
                    .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
                    .MinimumLength(1).WithMessage("Name must be at least 1 character");

                RuleFor(x => x.Name2)
                    .MaximumLength(100).WithMessage("Name2 must not exceed 100 characters");
            }
        }
    }

    partial class ApplicationLayer
    {
        private async Task<Result<Core.IApplicationResponseDTO>> ApplicationLogic(Sample sample, CancellationToken cancellationToken)
        {
            // Implement any further Application Code for the CreateSampleFeature here...
            // Use GetRequiredService or GetService to access accessable dependencies from this layer
            // ...
            
            // Implement business logic
            // ...
            
            var persistenceDTOResponse = await ForwardToInfrastructureLayer(sample.Adapt<CreateSamplePersistenceRequestDTO>(), cancellationToken);
            
            if (persistenceDTOResponse.IsError())
            {
                LogError(string.Join(", ", persistenceDTOResponse.Errors));
                return Result.Error(new ErrorList(persistenceDTOResponse.Errors));
            }

            return Result.Created((Core.IApplicationResponseDTO)persistenceDTOResponse.Value.Adapt<ApplicationResponseDTO>());
        }
        private sealed class SampleBusinessValidator : AbstractValidator<Sample>
        {
            public SampleBusinessValidator()
            {
                RuleFor(x => x.Name).NotEqual("Something forbidden by the business");
                RuleFor(x => x.Name2).NotEqual("Something forbidden by the business");
            }
        }
    }

    partial class InfrastructureLayer
    {
        private async Task<Result<Core.IPersistenceResponseDTO>> InfrastructureLogic(SampleEntity entity, CancellationToken cancellationToken)
        {
            // Implement any further Infrastructure Code for the CreateSampleFeature here
            // Use GetRequiredService or GetService to access accessable dependencies from this layer
            // ...

            // Implement infrastructure logic
            // ...
           
            var dbContext = GetRequiredDbContext<AppDbContext>();
            entity.CreatedAt = DateTime.UtcNow;

            dbContext.Set<SampleEntity>().Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (entity.Id == 0)
            {
                LogPersistenceFailed(entity.Adapt<PersistenceResponseDTO>().ToString()!);
                return Result.Error("Entity not persisted.");
            }

            var responseDto = entity.Adapt<PersistenceResponseDTO>();

            // Write-through: immediately populate the cache after a successful write so the
            // next GetSample request for this id is served from the cache without a database hit.
            var cache = GetRequiredService<IAppCache>();
            await cache.SetAsync(CachePayload.Create<SampleEntity>(entity.Id, entity), cancellationToken: cancellationToken);

            return Result.Created((Core.IPersistenceResponseDTO)responseDto);
        }
    }
}

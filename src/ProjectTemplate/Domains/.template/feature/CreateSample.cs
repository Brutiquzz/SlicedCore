using ProjectTemplate.Data;
using ProjectTemplate.Dependencies.Attributes;

namespace ProjectTemplate.Domains.Sample;

//#if (isCommand)
[Feature(FeatureType.Command)]
//#else
[Feature(FeatureType.Query)]
//#endif
public partial class CreateSample
{
    partial class PresentationLayer
    {
        private async Task<Result<ICreateSampleResponse>> PresentationLogic(ICreateSampleRequest request, CancellationToken cancellationToken)
        {
            // Implement any further Presentation Code for the CreateSampleFeature here...
            // Use GetRequired or Get to access accessible dependencies from this layer
            // ...

            var appDTOResponse = await ForwardToApplicationLayer(request.Adapt<Core.IApplicationRequestDTO>(), cancellationToken);

            if (appDTOResponse.IsError())
            {
                LogError(appDTOResponse.Errors);
                return Result.Error(new ErrorList(appDTOResponse.Errors));
            }

//#if (isCommand)
            return Result.Created((ICreateSampleResponse)appDTOResponse.Value.Adapt<CreateSampleResponse>());
//#else
            return Result.Success((ICreateSampleResponse)appDTOResponse.Value.Adapt<CreateSampleResponse>());
//#endif
        }

        public sealed class CreateSampleRequestValidator : AbstractValidator<ICreateSampleRequest>
        {
            public CreateSampleRequestValidator()
            {
//#if (isCommand)
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Name is required")
                    .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
                    .MinimumLength(1).WithMessage("Name must be at least 1 character");

                RuleFor(x => x.Name2)
                    .MaximumLength(100).WithMessage("Name2 must not exceed 100 characters");
//#else
                RuleFor(x => x.Id)
                    .GreaterThan(0).WithMessage("Id must be a positive integer");
//#endif
            }
        }
    }

    partial class ApplicationLayer
    {
        private async Task<Result<Core.IApplicationResponseDTO>> ApplicationLogic(Sample sample, CancellationToken cancellationToken)
        {
            // Implement any further Application Code for the CreateSampleFeature here...
            // Use GetRequiredService or GetService to access accessible dependencies from this layer
            // ...

            var persistenceDTOResponse = await ForwardToInfrastructureLayer(sample.Adapt<Core.IPersistenceRequestDTO>(), cancellationToken);

            if (persistenceDTOResponse.IsError())
            {
                LogError(string.Join(", ", persistenceDTOResponse.Errors));
                return Result.Error(new ErrorList(persistenceDTOResponse.Errors));
            }

//#if (isCommand)
            return Result.Created((Core.IApplicationResponseDTO)persistenceDTOResponse.Value.Adapt<ApplicationResponseDTO>());
//#else
            if (persistenceDTOResponse.Value is null)
                return Result.NotFound();

            return Result.Success((Core.IApplicationResponseDTO)persistenceDTOResponse.Value.Adapt<ApplicationResponseDTO>());
//#endif
        }

        private sealed class SampleBusinessValidator : AbstractValidator<Sample>
        {
            public SampleBusinessValidator()
            {
//#if (isCommand)
                RuleFor(x => x.Name).NotEqual("Something forbidden by the business");
                RuleFor(x => x.Name2).NotEqual("Something forbidden by the business");
//#else
                RuleFor(x => x.Id).GreaterThan(0);
//#endif
            }
        }
    }

    partial class InfrastructureLayer
    {
        private async Task<Result<Core.IPersistenceResponseDTO>> InfrastructureLogic(SampleEntity entity, CancellationToken cancellationToken)
        {
            // Implement any further Infrastructure Code for the CreateSampleFeature here
            // Use GetRequiredService or GetService to access accessible dependencies from this layer
            // ...

            var dbContext = GetRequiredDbContext<AppDbContext>();

//#if (isCommand)
            entity.CreatedAt = DateTime.UtcNow;

            dbContext.Set<SampleEntity>().Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (entity.Id == 0)
            {
                LogPersistenceFailed(entity.Adapt<PersistenceResponseDTO>().ToString());
                return Result.Error("Entity not persisted.");
            }

            return Result.Created((Core.IPersistenceResponseDTO)entity.Adapt<PersistenceResponseDTO>());
//#else
            var found = await dbContext.Set<SampleEntity>()
                .FindAsync(new object[] { entity.Id }, cancellationToken);

            if (found is null)
                return Result.NotFound();

            return Result.Success((Core.IPersistenceResponseDTO)found.Adapt<PersistenceResponseDTO>());
//#endif
        }
    }
}

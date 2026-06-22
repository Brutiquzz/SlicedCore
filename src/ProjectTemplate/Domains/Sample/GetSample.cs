using ProjectTemplate.Data;
using ProjectTemplate.Dependencies.Attributes;
using ProjectTemplate.Dependencies.Cache;

namespace ProjectTemplate.Domains.Sample;

[Feature(FeatureType.Query)]
public partial class GetSample
{
    partial class PresentationLayer
    {
        private async Task<Result<IGetSampleResponse>> PresentationLogic(IGetSampleRequest request, CancellationToken cancellationToken)
        {
            // Implement any further Presentation Code for the GetSampleFeature here...
            // Use GetRequiredService or GetService to access accessible dependencies from this layer
            // ...
            
            var appDTOResponse = await ForwardToApplicationLayer(request.Adapt<Core.IApplicationRequestDTO>(), cancellationToken);
            
            if (appDTOResponse.IsError())
            {
                LogError(appDTOResponse.Errors);
                return Result.Error(new ErrorList(appDTOResponse.Errors));
            }

            return Result.Success((IGetSampleResponse)appDTOResponse.Value.Adapt<GetSampleResponse>());
        }

        public sealed class GetSampleRequestValidator : AbstractValidator<IGetSampleRequest>
        {
            public GetSampleRequestValidator()
            {
                RuleFor(x => x.Id)
                    .GreaterThan(0).WithMessage("Id must be a positive integer");
            }
        }
    }

    partial class ApplicationLayer
    {
        private async Task<Result<Core.IApplicationResponseDTO>> ApplicationLogic(Sample sample, CancellationToken cancellationToken)
        {
            // Implement any further Application Code for the GetSampleFeature here...
            // Use GetRequiredService or GetService to access accessible dependencies from this layer
            // ...
            var persistenceDTOResponse = await ForwardToInfrastructureLayer(sample.Adapt<Core.IPersistenceRequestDTO>(), cancellationToken);
            
            if (persistenceDTOResponse.IsError())
            {
                LogError(string.Join(", ", persistenceDTOResponse.Errors));
                return Result.Error(new ErrorList(persistenceDTOResponse.Errors));
            }

            if (persistenceDTOResponse.Value is null)
                return Result.NotFound();

            return Result.Success((Core.IApplicationResponseDTO)persistenceDTOResponse.Value.Adapt<ApplicationResponseDTO>());
        }

        private sealed class SampleBusinessValidator : AbstractValidator<Sample>
        {
            public SampleBusinessValidator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
            }
        }
    }

    partial class InfrastructureLayer
    {
        private async Task<Result<Core.IPersistenceResponseDTO>> InfrastructureLogic(SampleEntity entity, CancellationToken cancellationToken)
        {
            // Read-through caching: check the cache first; on a miss, fetch from the database,
            // store the result, and return it. Subsequent reads for the same id are served
            // directly from the cache without hitting the database.
            var cache = GetRequiredService<IAppCache>();

            var cached = await cache.GetOrCreateAsync(
                CachePayload.KeyFor<SampleEntity>(entity.Id),
                async ct =>
                {
                    var dbContext = GetRequiredDbContext<AppDbContext>();
                    var found = await dbContext.Set<SampleEntity>()
                        .FindAsync(new object[] { entity.Id }, ct);
                    return found is null ? null : CachePayload.Create<SampleEntity>(found.Id, found);
                },
                cancellationToken: cancellationToken);

            if (cached is null)
                return Result.NotFound();

            var result = cached.Get<SampleEntity>();

            if (result is null)
                return Result.NotFound();

            return Result.Success((Core.IPersistenceResponseDTO)result.Adapt<PersistenceResponseDTO>());
        }
    }
}

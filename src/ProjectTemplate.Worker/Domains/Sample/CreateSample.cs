using ProjectTemplate.Client;
using ProjectTemplate.Dependencies.Attributes;
using System.Net;

namespace ProjectTemplate.Worker.Domains.Sample;

[Feature(FeatureType.Command)]
public partial class CreateSample
{
    partial class PresentationLayer
    {
        private async Task<Result<ICreateSampleResponse>> PresentationLogic(ICreateSampleRequest request, CancellationToken cancellationToken)
        {
            var applicationResponse = await ForwardToApplicationLayer(request.Adapt<CreateSampleRequestDTO>(), cancellationToken);

            if (applicationResponse.IsError())
            {
                LogError(applicationResponse.Errors);
                return Result.Error(new ErrorList(applicationResponse.Errors));
            }

            var persistenceResponse = await ForwardToInfrastructureLayer(
                applicationResponse.Value.Adapt<CreateSamplePersistenceRequestDTO>(),
                cancellationToken);

            if (persistenceResponse.IsError())
            {
                LogError(persistenceResponse.Errors);
                return Result.Error(new ErrorList(persistenceResponse.Errors));
            }

            return Result.Created((ICreateSampleResponse)persistenceResponse.Value.Adapt<CreateSampleResponse>());
        }

        private Task<Result<Core.IPersistenceResponseDTO>> ForwardToInfrastructureLayer(
            Core.IPersistenceRequestDTO request,
            CancellationToken cancellationToken)
            => ForwardToInfrastructureLayerCore<CreateSampleEventInfrastructureLayer, Result<Core.IPersistenceResponseDTO>>(
                new CreateSampleEventInfrastructureLayer(request),
                cancellationToken);

        public sealed class CreateSampleRequestValidator : AbstractValidator<ICreateSampleRequest>
        {
            public CreateSampleRequestValidator()
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
            var client = GetRequiredService<ProjectTemplateClient>();

            try
            {
                var response = await client.Sample.GetSample(sample.Id).Send(cancellationToken);
                return Result.Success((Core.IApplicationResponseDTO)response.Adapt<ApplicationResponseDTO>());
            }
            catch (ProjectTemplateClientApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.NotFound();
            }
            catch (ProjectTemplateClientApiException exception)
            {
                LogError(exception.Message);
                return Result.Error(exception.Message);
            }
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
            var client = GetRequiredService<ProjectTemplateClient>();

            try
            {
                var response = await client.Sample.CreateSample()
                    .WithName($"{entity.Name}-{Guid.NewGuid().ToString("N")[..8]}")
                    .WithName2(entity.Name2)
                    .Send(cancellationToken);

                return Result.Created((Core.IPersistenceResponseDTO)response.Adapt<PersistenceResponseDTO>());
            }
            catch (ProjectTemplateClientApiException exception)
            {
                LogPersistenceFailed(exception.Message);
                return Result.Error(exception.Message);
            }
        }
    }
}

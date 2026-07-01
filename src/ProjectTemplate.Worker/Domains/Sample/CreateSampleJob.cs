using ProjectTemplate.Client;

namespace ProjectTemplate.Worker.Domains.Sample;

[Feature(FeatureType.Command)]
public partial class CreateSampleJob
{
    partial class PresentationLayer
    {
        private async Task<Result<ICreateSampleJobResponse>> PresentationLogic(ICreateSampleJobRequest request, CancellationToken cancellationToken)
        {
            var appResponse = await ForwardToApplicationLayer(new CreateSampleJobRequestDTO
            {
                Name = $"{request.Name}-{request.ContextId}",
                Name2 = $"{request.Name2}-{request.ContextId}",
                ContextId = request.ContextId
            }, cancellationToken);

            if (appResponse.IsError())
            {
                LogError(appResponse.Errors);
                return Result.Error(new ErrorList(appResponse.Errors));
            }

            return Result.Created((ICreateSampleJobResponse)appResponse.Value.Adapt<CreateSampleJobResponse>());
        }

        /// <summary>Validates the raw job inputs: no field may be null, empty, or whitespace.</summary>
        public sealed class CreateSampleJobRequestValidator : AbstractValidator<ICreateSampleJobRequest>
        {
            public CreateSampleJobRequestValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Name must not be null or empty")
                    .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name must not be whitespace");

                RuleFor(x => x.Name2)
                    .NotEmpty().WithMessage("Name2 must not be null or empty")
                    .Must(name2 => !string.IsNullOrWhiteSpace(name2)).WithMessage("Name2 must not be whitespace");

                RuleFor(x => x.ContextId)
                    .NotEmpty().WithMessage("ContextId must not be null or empty")
                    .Must(id => !string.IsNullOrWhiteSpace(id)).WithMessage("ContextId must not be whitespace");
            }
        }
    }

    partial class ApplicationLayer
    {
        private async Task<Result<Core.IApplicationResponseDTO>> ApplicationLogic(Core.IApplicationRequestDTO request, CancellationToken cancellationToken)
        {
            var persistenceResponse = await ForwardToInfrastructureLayer(
                request.Adapt<CreateSampleJobPersistenceRequestDTO>(),
                cancellationToken);

            if (persistenceResponse.IsError())
            {
                LogError(string.Join(", ", persistenceResponse.Errors));
                return Result.Error(new ErrorList(persistenceResponse.Errors));
            }

            return Result.Created((Core.IApplicationResponseDTO)persistenceResponse.Value.Adapt<ApplicationResponseDTO>());
        }

        /// <summary>Validates business rules: the context ID appended to the names must be a valid GUID.</summary>
        private sealed class SampleBusinessValidator : AbstractValidator<Core.IApplicationRequestDTO>
        {
            public SampleBusinessValidator()
            {
                RuleFor(x => x.ContextId)
                    .Must(id => Guid.TryParse(id, out _))
                    .WithMessage("ContextId must be a valid GUID");
            }
        }
    }

    partial class InfrastructureLayer
    {
        private async Task<Result<Core.IPersistenceResponseDTO>> InfrastructureLogic(Core.IPersistenceRequestDTO request, CancellationToken cancellationToken)
        {
            var client = GetRequiredService<ProjectTemplateClient>();

            try
            {
                var response = await client.Sample.CreateSample()
                    .WithName(request.Name)
                    .WithName2(request.Name2)
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

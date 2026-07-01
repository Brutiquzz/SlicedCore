using TickerQ.DependencyInjection;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace ProjectTemplate.Worker.Domains.Sample;

public partial class CreateSampleJob
{
    /// <summary>Registers the recurring worker job for the sample feature.</summary>
    internal static IServiceCollection RegisterJob(IServiceCollection services)
    {
        services.MapTickerGroup("Sample", group =>
        {
            group.MapTicker<PresentationLayer.Job>("CreateSampleJob")
                .WithCron("0 * * * * *")
                .WithMaxConcurrency(1);
        });

        return services;
    }

    partial class PresentationLayer
    {
        /// <summary>TickerQ job that triggers the <c>CreateSampleJob</c> feature on a recurring schedule.</summary>
        internal sealed class Job(ILogger<Job> logger, IMediator mediator) : ITickerFunction
        {
            /// <inheritdoc />
            public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
            {
                logger.LogInformation("CreateSampleJob begun");

                try
                {
                    var result = await mediator.CreateSampleJob()
                        .WithName("SampleName")
                        .WithName2("SampleName2")
                        .WithContextId(context.Id.ToString())
                        .Send(cancellationToken);

                    if (result.IsError())
                    {
                        logger.LogError("CreateSampleJob failed: {Errors}", string.Join(", ", result.Errors));
                        throw new InvalidOperationException(string.Join(", ", result.Errors));
                    }

                    logger.LogInformation("CreateSampleJob completed");
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "CreateSampleJob failed");
                    throw;
                }
            }
        }
    }
}

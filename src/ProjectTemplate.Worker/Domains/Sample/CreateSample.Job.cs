using TickerQ.DependencyInjection;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace ProjectTemplate.Worker.Domains.Sample;

public partial class CreateSample
{
    /// <summary>Registers the recurring worker job for the sample feature.</summary>
    internal static IServiceCollection RegisterJob(IServiceCollection services)
    {
        services.MapTickerGroup("Sample", group =>
        {
            group.MapTicker<PresentationLayer.CreateSampleJob>("CreateSample")
                .WithCron("0 * * * * *")
                .WithMaxConcurrency(1);
        });

        return services;
    }

    partial class PresentationLayer
    {
        internal sealed class CreateSampleJob(ILogger<CreateSampleJob> logger, IMediator mediator) : ITickerFunction
        {
            public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
            {
                logger.LogInformation("job begun");

                try
                {
                    var result = await mediator.CreateSample()
                        .WithId(1)
                        .Send(cancellationToken);

                    if (result.IsError())
                    {
                        logger.LogError("job failed");
                        throw new InvalidOperationException(string.Join(", ", result.Errors));
                    }

                    logger.LogInformation("job completed");
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "job failed");
                    throw;
                }
            }
        }
    }
}

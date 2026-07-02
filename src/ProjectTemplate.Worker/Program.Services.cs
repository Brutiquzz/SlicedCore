using Cortex.Mediator.DependencyInjection;
using Microsoft.Extensions.Compliance.Redaction;
using ProjectTemplate.Client;
using ProjectTemplate.Compliance;
using ProjectTemplate.Dependencies;
using ProjectTemplate.Framework;
using ProjectTemplate.Worker.Domains.Sample;
using System.Reflection;
using TickerQ.DependencyInjection;

namespace ProjectTemplate.Worker;

public partial class Program
{
    /// <summary>Registers all worker services with the DI container.</summary>
    private static void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddRedaction(redaction =>
        {
            redaction.SetRedactor<PassThroughRedactor>(DataClassifications.Public);

            if (builder.Environment.IsDevelopment())
            {
                redaction.SetRedactor<PassThroughRedactor>(DataClassifications.Pii);
            }
            else
            {
                redaction.SetHmacRedactor(
                    builder.Configuration.GetSection("HmacRedactorOptions"),
                    DataClassifications.Pii);
            }
        });
        builder.Logging.EnableRedaction();

        builder.Services.AddCortexMediator(
            new[] { typeof(Program) },
            options => options.AddDefaultBehaviors());

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddLayerValidators(Assembly.GetExecutingAssembly());
        builder.Services.AddLayerHandlers(Assembly.GetExecutingAssembly());

        AddCoreLayerDependencies(new CoreLayerBuilder(builder));
        AddInfrastructureLayerDependencies(new InfrastructureLayerBuilder(builder));
        AddApplicationLayerDependencies(new ApplicationLayerBuilder(builder));
        AddPresentationLayerDependencies(new PresentationLayerBuilder(builder));

        var baseUri = builder.Configuration["ProjectTemplateClient:BaseUri"] ?? "https://localhost:5001/api/v1";
        builder.Services.AddProjectTemplateClient(new Uri(baseUri));

        builder.Services.AddTickerQ();
        CreateSampleJob.RegisterJob(builder.Services);
    }
}

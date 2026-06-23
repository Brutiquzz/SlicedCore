using ProjectTemplate.Client;
using ProjectTemplate.Dependencies;

namespace ProjectTemplate.Worker;

public partial class Program
{
    /// <summary>Registers presentation-layer-scoped dependencies for the worker.</summary>
    private static void AddPresentationLayerDependencies(PresentationLayerBuilder builder)
    {
    }

    /// <summary>Registers application-layer-scoped dependencies for the worker.</summary>
    private static void AddApplicationLayerDependencies(ApplicationLayerBuilder builder)
    {
        builder.AddScoped<ProjectTemplateClient>(serviceProvider => serviceProvider.GetRequiredService<ProjectTemplateClient>());
    }

    /// <summary>Registers infrastructure-layer-scoped dependencies for the worker.</summary>
    private static void AddInfrastructureLayerDependencies(InfrastructureLayerBuilder builder)
    {
        builder.AddScoped<ProjectTemplateClient>(serviceProvider => serviceProvider.GetRequiredService<ProjectTemplateClient>());
    }

    /// <summary>Registers core-layer-scoped dependencies for the worker.</summary>
    private static void AddCoreLayerDependencies(CoreLayerBuilder builder)
    {
    }
}

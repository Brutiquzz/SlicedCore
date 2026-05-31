using Microsoft.EntityFrameworkCore;
using ProjectTemplate.Data;
using ProjectTemplate.Dependencies;

namespace ProjectTemplate;

public partial class Program
{
    /// <summary>Registers presentation-layer-scoped dependencies for the application.</summary>
    private static void AddPresentationLayerDependencies(PresentationLayerBuilder builder)
    {
        // Add Presentation Layered dependencies
        // ...

        // builder.AddTransient<IPresentationService, PresentationService>();
    }

    /// <summary>Registers application-layer-scoped dependencies for the application.</summary>
    private static void AddApplicationLayerDependencies(ApplicationLayerBuilder builder)
    {
        // Add Application Layered dependencies
        // ...

        // builder.AddTransient<IApplicationService, ApplicationService>();
    }

    /// <summary>Registers infrastructure-layer-scoped dependencies, including the EF Core DbContext.</summary>
    private static void AddInfrastructureLayerDependencies(InfrastructureLayerBuilder builder)
    {
        // Add Infrastructure Layered dependencies
        // ...

        builder.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    }

    /// <summary>Registers core/domain-layer-scoped dependencies for the application.</summary>
    private static void AddCoreLayerDependencies(CoreLayerBuilder builder)
    {
        // Add Domain Layered dependencies
        // ...
        // builder.AddTransient<IDomainService, DomainService>();
    }
}

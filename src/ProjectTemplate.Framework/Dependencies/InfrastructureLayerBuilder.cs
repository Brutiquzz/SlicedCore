using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ProjectTemplate.Dependencies.Attributes;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// A scoped service registration surface that only allows registering
/// infrastructure-layer-keyed dependencies and infrastructure-specific
/// framework services (e.g. DbContext). Raw IServiceCollection access
/// is intentionally not exposed so registrations cannot accidentally
/// bypass the layer boundary.
/// </summary>
public sealed class InfrastructureLayerBuilder
{
    private readonly IHostApplicationBuilder _builder;

    /// <summary>Provides read-only access to the application configuration for use during service registration.</summary>
    public IConfiguration Configuration => _builder.Configuration;

    /// <param name="builder">The web application builder to register services into.</param>
    public InfrastructureLayerBuilder(IHostApplicationBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient infrastructure-layer-keyed <typeparamref name="TService"/>.</summary>
    public InfrastructureLayerBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddTransientInfrastructureDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers a transient infrastructure-layer-keyed <typeparamref name="TService"/> using a factory.</summary>
    public InfrastructureLayerBuilder AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _builder.Services.AddTransientInfrastructureDependency(implementationFactory);
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped infrastructure-layer-keyed <typeparamref name="TService"/>.</summary>
    public InfrastructureLayerBuilder AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddScopedInfrastructureDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers a scoped infrastructure-layer-keyed <typeparamref name="TService"/> using a factory.</summary>
    public InfrastructureLayerBuilder AddScoped<TService>(Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _builder.Services.AddScopedInfrastructureDependency(implementationFactory);
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton infrastructure-layer-keyed <typeparamref name="TService"/>.</summary>
    public InfrastructureLayerBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddSingletonInfrastructureDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers a singleton infrastructure-layer-keyed <typeparamref name="TService"/> using a factory.</summary>
    public InfrastructureLayerBuilder AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _builder.Services.AddSingletonInfrastructureDependency(implementationFactory);
        return this;
    }

    /// <summary>
    /// Registers an EF Core DbContext.
    /// <para>
    /// EF Core requires the context to be registered as an unkeyed scoped service —
    /// this cannot be changed. However, an <see cref="InfrastructureDbContext{TContext}"/>
    /// wrapper is additionally registered as a keyed infrastructure service, which is
    /// the only path exposed via <c>GetRequiredService&lt;InfrastructureDbContext&lt;T&gt;&gt;()</c>
    /// inside <see cref="Infrastructure"/>-derived classes. The context type itself is
    /// internal, so it cannot be injected directly into controllers or other layers.
    /// </para>
    /// </summary>
    public InfrastructureLayerBuilder AddDbContext<TContext>(Action<DbContextOptionsBuilder> optionsAction)
        where TContext : DbContext
    {
        // EF Core's own unkeyed registration — required for migrations, pooling, design-time tools.
        _builder.Services.AddDbContext<TContext>(optionsAction);

        // Keyed wrapper — the only surface InfrastructureLayer code should resolve.
        _builder.Services.AddKeyedScoped(
            ServiceKeys.GetKey(ServiceLayer.Infrastructure),
            (sp, _) => new InfrastructureDbContext<TContext>(sp.GetRequiredService<TContext>()));

        return this;
    }
}

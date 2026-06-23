using Microsoft.Extensions.Hosting;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// A scoped service registration surface that only allows registering
/// application-layer-keyed dependencies, preventing accidental registrations
/// in the wrong layer.
/// </summary>
public sealed class ApplicationLayerBuilder
{
    private readonly IHostApplicationBuilder _builder;

    /// <summary>Provides read-only access to the application configuration for use during service registration.</summary>
    public IConfiguration Configuration => _builder.Configuration;

    /// <param name="builder">The web application builder to register services into.</param>
    public ApplicationLayerBuilder(IHostApplicationBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient application-layer-keyed <typeparamref name="TService"/>.</summary>
    public ApplicationLayerBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddTransientApplicationDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers a transient application-layer-keyed <typeparamref name="TService"/> using a factory.</summary>
    public ApplicationLayerBuilder AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _builder.Services.AddTransientApplicationDependency(implementationFactory);
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped application-layer-keyed <typeparamref name="TService"/>.</summary>
    public ApplicationLayerBuilder AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddScopedApplicationDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers a scoped application-layer-keyed <typeparamref name="TService"/> using a factory.</summary>
    public ApplicationLayerBuilder AddScoped<TService>(Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _builder.Services.AddScopedApplicationDependency(implementationFactory);
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton application-layer-keyed <typeparamref name="TService"/>.</summary>
    public ApplicationLayerBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddSingletonApplicationDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers a singleton application-layer-keyed <typeparamref name="TService"/> using a factory.</summary>
    public ApplicationLayerBuilder AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        _builder.Services.AddSingletonApplicationDependency(implementationFactory);
        return this;
    }
}

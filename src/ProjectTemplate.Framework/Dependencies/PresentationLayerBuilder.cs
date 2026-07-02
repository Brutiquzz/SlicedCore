using Microsoft.Extensions.Hosting;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// A scoped service registration surface that only allows registering
/// presentation-layer-keyed dependencies, preventing accidental registrations
/// in the wrong layer.
/// </summary>
public sealed class PresentationLayerBuilder
{
    private readonly IHostApplicationBuilder _builder;

    /// <summary>Provides read-only access to the application configuration for use during service registration.</summary>
    public IConfiguration Configuration => _builder.Configuration;

    /// <param name="builder">The web application builder to register services into.</param>
    public PresentationLayerBuilder(IHostApplicationBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient presentation-layer-keyed <typeparamref name="TService"/>.</summary>
    public PresentationLayerBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddTransientPresentationDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped presentation-layer-keyed <typeparamref name="TService"/>.</summary>
    public PresentationLayerBuilder AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddScopedPresentationDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton presentation-layer-keyed <typeparamref name="TService"/>.</summary>
    public PresentationLayerBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddSingletonPresentationDependency<TService, TImplementation>();
        return this;
    }
}

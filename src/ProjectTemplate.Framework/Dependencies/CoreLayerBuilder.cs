namespace ProjectTemplate.Dependencies;

/// <summary>
/// A scoped service registration surface that only allows registering
/// core-layer-keyed dependencies, preventing accidental registrations
/// in the wrong layer.
/// </summary>
public sealed class CoreLayerBuilder
{
    private readonly WebApplicationBuilder _builder;

    /// <summary>Provides read-only access to the application configuration for use during service registration.</summary>
    public IConfiguration Configuration => _builder.Configuration;

    /// <param name="builder">The web application builder to register services into.</param>
    public CoreLayerBuilder(WebApplicationBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient core-layer-keyed <typeparamref name="TService"/>.</summary>
    public CoreLayerBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddTransientCoreDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped core-layer-keyed <typeparamref name="TService"/>.</summary>
    public CoreLayerBuilder AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddScopedCoreDependency<TService, TImplementation>();
        return this;
    }

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton core-layer-keyed <typeparamref name="TService"/>.</summary>
    public CoreLayerBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _builder.Services.AddSingletonCoreDependency<TService, TImplementation>();
        return this;
    }
}

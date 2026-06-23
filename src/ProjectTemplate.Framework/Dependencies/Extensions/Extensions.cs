using Microsoft.Extensions.DependencyInjection;
using ProjectTemplate.Dependencies.Attributes;

namespace ProjectTemplate.Dependencies.Extensions;
/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> and <see cref="IServiceProvider"/>
/// that register and resolve services using layer-specific DI keys.
/// Use these instead of the raw keyed-service APIs to keep layer boundaries explicit.
/// </summary>
public static class ServiceCollectionLayerExtensions
{
    // ---------- Generic with Service + Implementation ----------
    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient presentation-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddTransientPresentationDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedTransient<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient application-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddTransientApplicationDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedTransient<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient core-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddTransientCoreDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedTransient<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Core));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a transient infrastructure-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddTransientInfrastructureDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedTransient<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped presentation-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddScopedPresentationDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedScoped<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped application-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddScopedApplicationDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedScoped<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped core-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddScopedCoreDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedScoped<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Core));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a scoped infrastructure-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddScopedInfrastructureDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedScoped<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton presentation-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddSingletonPresentationDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedSingleton<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton application-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddSingletonApplicationDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedSingleton<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton core-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddSingletonCoreDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedSingleton<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Core));

    /// <summary>Registers <typeparamref name="TImplementation"/> as a singleton infrastructure-layer-keyed <typeparamref name="TService"/>.</summary>
    public static IServiceCollection AddSingletonInfrastructureDependency<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        => services.AddKeyedSingleton<TService, TImplementation>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));

    // ---------- Overload for just Implementation (self-registration) ----------
    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a transient presentation-layer-keyed service.</summary>
    public static IServiceCollection AddTransientPresentationDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedTransient<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Registers a transient presentation-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddTransientPresentationDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedTransient<TService>(
            ServiceKeys.GetKey(ServiceLayer.Presentation),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a transient application-layer-keyed service.</summary>
    public static IServiceCollection AddTransientApplicationDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedTransient<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Registers a transient application-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddTransientApplicationDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedTransient<TService>(
            ServiceKeys.GetKey(ServiceLayer.Application),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a transient core-layer-keyed service.</summary>
    public static IServiceCollection AddTransientCoreDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedTransient<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Core));

    /// <summary>Registers a transient core-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddTransientCoreDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedTransient<TService>(
            ServiceKeys.GetKey(ServiceLayer.Core),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a transient infrastructure-layer-keyed service.</summary>
    public static IServiceCollection AddTransientInfrastructureDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedTransient<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));

    /// <summary>Registers a transient infrastructure-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddTransientInfrastructureDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedTransient<TService>(
            ServiceKeys.GetKey(ServiceLayer.Infrastructure),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a scoped presentation-layer-keyed service.</summary>
    public static IServiceCollection AddScopedPresentationDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedScoped<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Registers a scoped presentation-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddScopedPresentationDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedScoped<TService>(
            ServiceKeys.GetKey(ServiceLayer.Presentation),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a scoped application-layer-keyed service.</summary>
    public static IServiceCollection AddScopedApplicationDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedScoped<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Registers a scoped application-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddScopedApplicationDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedScoped<TService>(
            ServiceKeys.GetKey(ServiceLayer.Application),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a scoped core-layer-keyed service.</summary>
    public static IServiceCollection AddScopedCoreDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedScoped<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Core));

    /// <summary>Registers a scoped core-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddScopedCoreDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedScoped<TService>(
            ServiceKeys.GetKey(ServiceLayer.Core),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a scoped infrastructure-layer-keyed service.</summary>
    public static IServiceCollection AddScopedInfrastructureDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedScoped<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));

    /// <summary>Registers a scoped infrastructure-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddScopedInfrastructureDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedScoped<TService>(
            ServiceKeys.GetKey(ServiceLayer.Infrastructure),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a singleton presentation-layer-keyed service.</summary>
    public static IServiceCollection AddSingletonPresentationDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedSingleton<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Registers a singleton presentation-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddSingletonPresentationDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedSingleton<TService>(
            ServiceKeys.GetKey(ServiceLayer.Presentation),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a singleton application-layer-keyed service.</summary>
    public static IServiceCollection AddSingletonApplicationDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedSingleton<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Registers a singleton application-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddSingletonApplicationDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedSingleton<TService>(
            ServiceKeys.GetKey(ServiceLayer.Application),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a singleton core-layer-keyed service.</summary>
    public static IServiceCollection AddSingletonCoreDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedSingleton<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Core));

    /// <summary>Registers a singleton core-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddSingletonCoreDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedSingleton<TService>(
            ServiceKeys.GetKey(ServiceLayer.Core),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Self-registers <typeparamref name="TImplementation"/> as a singleton infrastructure-layer-keyed service.</summary>
    public static IServiceCollection AddSingletonInfrastructureDependency<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class
        => services.AddKeyedSingleton<TImplementation>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));

    /// <summary>Registers a singleton infrastructure-layer-keyed service using a factory.</summary>
    public static IServiceCollection AddSingletonInfrastructureDependency<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
        => services.AddKeyedSingleton<TService>(
            ServiceKeys.GetKey(ServiceLayer.Infrastructure),
            (serviceProvider, _) => implementationFactory(serviceProvider));

    /// <summary>Resolves a required presentation-layer-keyed <typeparamref name="TService"/> from the service provider.</summary>
    public static TService GetRequiredPresentationDependency<TService>(this IServiceProvider serviceProvider)
        where TService : class
        => serviceProvider.GetRequiredKeyedService<TService>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Resolves an optional presentation-layer-keyed <typeparamref name="TService"/>, or <c>null</c>.</summary>
    public static TService? GetPresentationDependency<TService>(this IServiceProvider serviceProvider)
        where TService : class
        => serviceProvider.GetKeyedService<TService>(ServiceKeys.GetKey(ServiceLayer.Presentation));

    /// <summary>Resolves a required application-layer-keyed <typeparamref name="TService"/> from the service provider.</summary>
    public static TService GetRequiredApplicationDependency<TService>(this IServiceProvider serviceProvider)
        where TService : class
        => serviceProvider.GetRequiredKeyedService<TService>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Resolves an optional application-layer-keyed <typeparamref name="TService"/>, or <c>null</c>.</summary>
    public static TService? GetApplicationDependency<TService>(this IServiceProvider serviceProvider)
        where TService : class
        => serviceProvider.GetKeyedService<TService>(ServiceKeys.GetKey(ServiceLayer.Application));

    /// <summary>Resolves a required core-layer-keyed <typeparamref name="TService"/> from the service provider.</summary>
    public static TService GetRequiredCoreDependency<TService>(this IServiceProvider serviceProvider)
        where TService : class
        => serviceProvider.GetRequiredKeyedService<TService>(ServiceKeys.GetKey(ServiceLayer.Core));

    /// <summary>Resolves a required infrastructure-layer-keyed <typeparamref name="TService"/> from the service provider.</summary>
    public static TService GetRequiredInfrastructureDependency<TService>(this IServiceProvider serviceProvider)
        where TService : class
        => serviceProvider.GetRequiredKeyedService<TService>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));

    public static TService? GetInfrastructureDependency<TService>(this IServiceProvider serviceProvider)
        where TService : class
        => serviceProvider.GetKeyedService<TService>(ServiceKeys.GetKey(ServiceLayer.Infrastructure));
}

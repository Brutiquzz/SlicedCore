using Cortex.Mediator;
using FluentValidation;
using ProjectTemplate.Dependencies.Attributes;
using System.Reflection;

namespace ProjectTemplate.Framework;

/// <summary>
/// Assembly-scanning helpers that register command handlers and layer-scoped
/// FluentValidation validators with the DI container.
/// </summary>
public static class LayerHandlerScanner
{
    /// <summary>
    /// Scans <paramref name="assembly"/> for <see cref="ICommandHandler{TCommand,TResult}"/> implementations
    /// and registers them as transient services — once for the concrete command type and once for each
    /// command interface the concrete type implements (so the mediator can resolve by interface).
    /// </summary>
    /// <param name="services">The service collection to register handlers into.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddLayerHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => t.GetInterfaces().Any(IsCommandHandlerInterface));

        foreach (var handlerType in handlerTypes)
        {
            foreach (var handlerInterface in handlerType.GetInterfaces().Where(IsCommandHandlerInterface))
            {
                var genericArgs = handlerInterface.GetGenericArguments();
                var commandType = genericArgs[0];
                var resultType = genericArgs[1];

                // Register for the concrete command type
                services.AddTransient(handlerInterface, handlerType);

                // Register for each ICommand interface the concrete command implements
                // This resolves the interface-vs-concrete mismatch the mediator encounters
                // Skip the raw ICommand<> base interface — it is too generic and causes DI validation errors
                foreach (var commandInterface in commandType.GetInterfaces()
                    .Where(i => !(i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))))
                {
                    Type interfacedHandlerServiceType;
                    try
                    {
                        interfacedHandlerServiceType = typeof(ICommandHandler<,>)
                            .MakeGenericType(commandInterface, resultType);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    services.AddTransient(interfacedHandlerServiceType, handlerType);
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Scans the assembly for FluentValidation validators nested inside layer classes
    /// (e.g. <c>PresentationLayer</c>, <c>ApplicationLayer</c>) and registers them as
    /// keyed services under the appropriate layer key so they can be resolved via
    /// <see cref="Dependencies.Presentation.GetRequiredService{T}"/> /
    /// <see cref="Dependencies.Application.GetRequiredService{T}"/>.
    /// </summary>
    public static IServiceCollection AddLayerValidators(this IServiceCollection services, Assembly assembly)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)));

        foreach (var validatorType in validatorTypes)
        {
            var declaringType = validatorType.DeclaringType;
            if (declaringType is null) continue;

            var layerKey = GetLayerKey(declaringType.Name);
            if (layerKey is null) continue;

            services.AddKeyedTransient(validatorType, layerKey, validatorType);
        }

        return services;
    }

    private static object? GetLayerKey(string declaringTypeName) => declaringTypeName switch
    {
        "PresentationLayer" => ServiceKeys.GetKey(ServiceLayer.Presentation),
        "ApplicationLayer"  => ServiceKeys.GetKey(ServiceLayer.Application),
        "InfrastructureLayer" => ServiceKeys.GetKey(ServiceLayer.Infrastructure),
        "CoreLayer"         => ServiceKeys.GetKey(ServiceLayer.Core),
        _ => null
    };

    private static bool IsCommandHandlerInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>);
}

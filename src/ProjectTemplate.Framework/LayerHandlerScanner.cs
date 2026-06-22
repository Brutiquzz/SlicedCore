using Cortex.Mediator;
using Cortex.Mediator.Queries;
using FluentValidation;
using ProjectTemplate.Dependencies.Attributes;
using System.Reflection;

namespace ProjectTemplate.Framework;

/// <summary>
/// Assembly-scanning helpers that register command/query handlers and layer-scoped
/// FluentValidation validators with the DI container.
/// </summary>
public static class LayerHandlerScanner
{
    /// <summary>
    /// Scans <paramref name="assembly"/> for <see cref="ICommandHandler{TCommand,TResult}"/> and
    /// <see cref="IQueryHandler{TQuery,TResult}"/> implementations and registers them as transient
    /// services — once for the declared event type and once for each concrete event type that
    /// implements the declared interface (so the mediator can resolve by the concrete record type
    /// that is dispatched at runtime).
    /// </summary>
    /// <param name="services">The service collection to register handlers into.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddLayerHandlers(this IServiceCollection services, Assembly assembly)
    {
        RegisterHandlers(
            services,
            assembly,
            IsCommandHandlerInterface,
            typeof(ICommandHandler<,>),
            skipBaseEventInterface: typeof(ICommand<>));

        RegisterHandlers(
            services,
            assembly,
            IsQueryHandlerInterface,
            typeof(IQueryHandler<,>),
            skipBaseEventInterface: typeof(IQuery<>));

        return services;
    }

    private static void RegisterHandlers(
        IServiceCollection services,
        Assembly assembly,
        Func<Type, bool> isHandlerInterface,
        Type openHandlerType,
        Type skipBaseEventInterface)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => t.GetInterfaces().Any(isHandlerInterface));

        foreach (var handlerType in handlerTypes)
        {
            foreach (var handlerInterface in handlerType.GetInterfaces().Where(isHandlerInterface))
            {
                var genericArgs = handlerInterface.GetGenericArguments();
                var eventType = genericArgs[0];
                var resultType = genericArgs[1];

                // Register for the declared event type (may be an interface)
                services.AddTransient(handlerInterface, handlerType);

                // If eventType is an interface, also register for every concrete type in the
                // assembly that implements it — the mediator dispatches with the concrete event record.
                if (eventType.IsInterface)
                {
                    foreach (var concreteEvent in assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && eventType.IsAssignableFrom(t)))
                    {
                        Type concreteHandlerServiceType;
                        try
                        {
                            concreteHandlerServiceType = openHandlerType
                                .MakeGenericType(concreteEvent, resultType);
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }

                        services.AddTransient(concreteHandlerServiceType, handlerType);
                    }
                }

                // Also register for each event interface the concrete event implements
                // (skipping the raw base interface to avoid DI validation errors)
                foreach (var eventInterface in eventType.GetInterfaces()
                    .Where(i => !(i.IsGenericType && i.GetGenericTypeDefinition() == skipBaseEventInterface)))
                {
                    Type interfacedHandlerServiceType;
                    try
                    {
                        interfacedHandlerServiceType = openHandlerType
                            .MakeGenericType(eventInterface, resultType);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    services.AddTransient(interfacedHandlerServiceType, handlerType);
                }
            }
        }
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

    private static bool IsQueryHandlerInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);
}

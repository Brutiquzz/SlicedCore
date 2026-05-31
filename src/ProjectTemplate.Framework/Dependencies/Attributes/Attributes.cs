namespace ProjectTemplate.Dependencies.Attributes;

using System;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Base attribute that resolves a constructor parameter from the keyed DI container
/// using the key for the specified <see cref="ServiceLayer"/>.
/// Use the concrete derived attributes instead of this base class directly.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromLayerDependencyAttribute : FromKeyedServicesAttribute
{
    /// <param name="layer">The architectural layer whose DI key should be used to resolve the parameter.</param>
    public FromLayerDependencyAttribute(ServiceLayer layer)
        : base(ServiceKeys.GetKey(layer))
    {
    }
}

/// <summary>
/// Resolves a constructor parameter from the presentation-layer keyed DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PresentationLayerDependencyAttribute : FromLayerDependencyAttribute
{
    /// <inheritdoc />
    public PresentationLayerDependencyAttribute()
        : base(ServiceLayer.Presentation) { }
}

/// <summary>
/// Resolves a constructor parameter from the application-layer keyed DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ApplicationLayerDependencyAttribute : FromLayerDependencyAttribute
{
    /// <inheritdoc />
    public ApplicationLayerDependencyAttribute()
        : base(ServiceLayer.Application) { }
}

/// <summary>
/// Resolves a constructor parameter from the infrastructure-layer keyed DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class InfrastructureLayerDependencyAttribute : FromLayerDependencyAttribute
{
    /// <inheritdoc />
    public InfrastructureLayerDependencyAttribute()
        : base(ServiceLayer.Infrastructure) { }
}

/// <summary>
/// Resolves a constructor parameter from the core/domain-layer keyed DI container.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class CoreLayerDependencyAttribute : FromLayerDependencyAttribute
{
    /// <inheritdoc />
    public CoreLayerDependencyAttribute()
        : base(ServiceLayer.Core) { }
}


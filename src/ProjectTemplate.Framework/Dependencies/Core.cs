using System.ComponentModel;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// Base class for core/domain-layer components.
/// Provides a construction hook for injecting shared domain-level services.
/// </summary>
/// <remarks>
/// Inherit from this class in your domain model or core partial classes.
/// Direct instantiation is not intended — the DI container creates instances.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class Core
{
    /// <summary>
    /// Inject any dependencies that are needed for the Core layer.
    /// </summary>
    protected Core(IServiceProvider serviceProvider)
    {
        // A mapper or other Core services could be injected here
    }
}

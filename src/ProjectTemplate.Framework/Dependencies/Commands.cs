using System.ComponentModel;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// Typed command dispatcher available to <c>InfrastructureLayer</c> handlers of command features.
/// Use extension methods generated for each command feature (e.g. <c>commands.CreateSample().WithName("x").Send(ct)</c>)
/// rather than accessing <see cref="Inner"/> directly.
/// </summary>
public readonly struct Commands
{
    /// <summary>The underlying mediator. For use by generated extension methods only.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly IMediator Inner;

    internal Commands(IMediator mediator)
    {
        Inner = mediator;
    }
}

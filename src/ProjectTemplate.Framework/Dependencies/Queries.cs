using System.ComponentModel;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// Typed query dispatcher available to <c>ApplicationLayer</c> handlers.
/// Use extension methods generated for each query feature (e.g. <c>queries.GetSample().WithId(1).Send(ct)</c>)
/// rather than accessing <see cref="Inner"/> directly.
/// </summary>
public readonly struct Queries
{
    /// <summary>The underlying mediator. For use by generated extension methods only.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly IMediator Inner;

    internal Queries(IMediator mediator)
    {
        Inner = mediator;
    }
}

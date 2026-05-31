using Microsoft.EntityFrameworkCore;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// A keyed infrastructure-layer wrapper around an EF Core <typeparamref name="TContext"/>.
/// This is the only publicly nameable path to a DbContext from within
/// <see cref="Infrastructure"/>-derived classes. The underlying context type
/// is internal, so it cannot be injected directly into controllers or other layers.
/// </summary>
public sealed class InfrastructureDbContext<TContext> where TContext : DbContext
{
    internal TContext Value { get; }

    internal InfrastructureDbContext(TContext context) => Value = context;
}

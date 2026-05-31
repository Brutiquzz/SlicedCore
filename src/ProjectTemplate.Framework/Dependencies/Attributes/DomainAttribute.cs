namespace ProjectTemplate.Dependencies.Attributes;

/// <summary>
/// Marks a class as a domain container that groups business models, persistence models,
/// and their EF Core configurations for a single bounded context.
/// The source generator scans for nested interfaces annotated with
/// <see cref="BusinessModelAttribute"/> and <see cref="PersistenceModelAttribute"/> to
/// generate concrete types and <c>DbContext</c> configuration hooks.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DomainAttribute : Attribute
{
}

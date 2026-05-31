namespace ProjectTemplate.Dependencies.Attributes;

/// <summary>
/// Marks an interface as a persistence/entity model contract for a feature.
/// Applied to interfaces nested inside a <see cref="DomainAttribute"/>-decorated class.
/// The source generator uses this to emit a concrete entity class implementing the interface
/// and registers it with the EF Core model builder.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class PersistenceModelAttribute : Attribute
{
}

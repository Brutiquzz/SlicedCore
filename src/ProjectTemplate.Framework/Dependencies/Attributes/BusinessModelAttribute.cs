namespace ProjectTemplate.Dependencies.Attributes;

/// <summary>
/// Marks an interface as the business/domain model contract for a feature.
/// Applied to interfaces nested inside a <see cref="DomainAttribute"/>-decorated class.
/// The source generator uses this to emit a concrete record implementing the interface.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class BusinessModelAttribute : Attribute
{
}

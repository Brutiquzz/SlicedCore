namespace ProjectTemplate.Dependencies.Attributes;

/// <summary>
/// Marks a static method as an EF Core model configuration callback for a specific
/// <see cref="PersistenceModelAttribute"/>-annotated interface.
/// The source generator discovers these methods and calls them from <c>OnModelCreating</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PersistenceModelConfigurationAttribute : Attribute
{
    /// <summary>
    /// The persistence model interface whose EF Core entity this method configures.
    /// </summary>
    public Type ModelInterface { get; }

    /// <param name="modelInterface">The persistence model interface to configure.</param>
    public PersistenceModelConfigurationAttribute(Type modelInterface)
    {
        ModelInterface = modelInterface;
    }
}

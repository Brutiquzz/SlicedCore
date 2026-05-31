namespace ProjectTemplate.Dependencies.Attributes;

/// <summary>
/// Identifies the architectural layer a service belongs to.
/// Used as the DI key when registering and resolving layer-scoped dependencies
/// via <see cref="ServiceKeys"/>.
/// </summary>
public enum ServiceLayer
{
    /// <summary>The presentation layer — endpoints, request/response mapping, and input validation.</summary>
    Presentation,
    /// <summary>The application layer — orchestration, use-case logic, and cross-cutting concerns.</summary>
    Application,
    /// <summary>The infrastructure layer — data access, external services, and I/O.</summary>
    Infrastructure,
    /// <summary>The core/domain layer — business rules and domain models.</summary>
    Core
}

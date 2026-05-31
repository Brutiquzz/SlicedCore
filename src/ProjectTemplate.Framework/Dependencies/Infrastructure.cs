using System.ComponentModel;
using Microsoft.Extensions.Compliance.Redaction;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// Base class for infrastructure-layer handlers.
/// Provides access to the mediator, logger, redactor provider, and
/// infrastructure-layer-keyed dependency resolution including DbContext access.
/// </summary>
/// <remarks>
/// Inherit from this class in your infrastructure-layer handler partial classes.
/// Direct instantiation is not intended — the DI container creates instances.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class Infrastructure
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMediator mediator;
    /// <summary>Logger scoped to the concrete handler type.</summary>
    protected readonly ILogger logger;
    /// <summary>Redactor provider used to mask PII before writing to logs.</summary>
    protected readonly IRedactorProvider redactorProvider;
    /// <summary>
    /// Command dispatcher. Available to infrastructure layers of command features.
    /// Shadowed with <c>[Obsolete(error: true)]</c> in generated query infrastructure boilerplate.
    /// </summary>
    protected readonly Commands commands;

    /// <summary>
    /// Inject any dependencies that are needed for the infrastructure layer.
    /// </summary>
    protected Infrastructure(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.mediator = serviceProvider.GetRequiredService<IMediator>();
        this.logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        this.redactorProvider = serviceProvider.GetRequiredService<IRedactorProvider>();
        this.commands = new Commands(this.mediator);
    }

    /// <summary>Resolves a required infrastructure-layer-keyed service of type <typeparamref name="T"/>.</summary>
    protected T GetRequiredService<T>() where T : class
        => serviceProvider.GetRequiredInfrastructureDependency<T>();

    /// <summary>Resolves an optional infrastructure-layer-keyed service of type <typeparamref name="T"/>, or <c>null</c>.</summary>
    protected T? GetService<T>() where T : class
        => serviceProvider.GetInfrastructureDependency<T>();

    /// <summary>
    /// Resolves the EF Core DbContext of type <typeparamref name="TContext"/> through
    /// the infrastructure-keyed <see cref="InfrastructureDbContext{TContext}"/> wrapper.
    /// This is the only supported way to access a DbContext from within an infrastructure handler.
    /// </summary>
    protected TContext GetRequiredDbContext<TContext>() where TContext : Microsoft.EntityFrameworkCore.DbContext
        => serviceProvider.GetRequiredInfrastructureDependency<InfrastructureDbContext<TContext>>().Value;
}

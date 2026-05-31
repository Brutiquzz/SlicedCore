using System.ComponentModel;
using Microsoft.Extensions.Compliance.Redaction;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// Base class for application-layer handlers.
/// Provides access to the mediator, logger, and redactor provider, as well as
/// application-layer-keyed dependency resolution.
/// </summary>
/// <remarks>
/// Inherit from this class in your application-layer handler partial classes.
/// Direct instantiation is not intended — the DI container creates instances.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class Application
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMediator mediator;
    /// <summary>Logger scoped to the concrete handler type.</summary>
    protected readonly ILogger logger;
    /// <summary>Redactor provider used to mask PII before writing to logs.</summary>
    protected readonly IRedactorProvider redactorProvider;
    /// <summary>Query dispatcher. Use this to invoke query features from within an application layer.</summary>
    protected readonly Queries queries;

    /// <summary>
    /// Forwards a command to the infrastructure layer via the mediator.
    /// Used exclusively by generated <c>ForwardToInfrastructureLayer</c> boilerplate.
    /// </summary>
    protected global::System.Threading.Tasks.Task<TResult> ForwardCommand<TCommand, TResult>(
        TCommand command,
        global::System.Threading.CancellationToken cancellationToken)
        where TCommand : global::Cortex.Mediator.Commands.ICommand<TResult>
        => mediator.SendCommandAsync<TCommand, TResult>(command, cancellationToken);

    /// <summary>
    /// Forwards a query to the infrastructure layer via the mediator.
    /// Used exclusively by generated <c>ForwardToInfrastructureLayer</c> boilerplate on query features.
    /// </summary>
    protected global::System.Threading.Tasks.Task<TResult> ForwardQuery<TQuery, TResult>(
        TQuery query,
        global::System.Threading.CancellationToken cancellationToken)
        where TQuery : global::Cortex.Mediator.Queries.IQuery<TResult>
        => mediator.SendQueryAsync<TQuery, TResult>(query, cancellationToken);

    /// <summary>
    /// Inject any dependencies that are needed for the application layer.
    /// </summary>
    protected Application(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.mediator = serviceProvider.GetRequiredService<IMediator>();
        this.logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        this.redactorProvider = serviceProvider.GetRequiredService<IRedactorProvider>();
        this.queries = new Queries(this.mediator);
    }

    /// <summary>Resolves a required application-layer-keyed service of type <typeparamref name="T"/>.</summary>
    protected T GetRequiredService<T>() where T : class
        => serviceProvider.GetRequiredApplicationDependency<T>();

    /// <summary>Resolves an optional application-layer-keyed service of type <typeparamref name="T"/>, or <c>null</c>.</summary>
    protected T? GetService<T>() where T : class
        => serviceProvider.GetApplicationDependency<T>();
}

using System.ComponentModel;
using Microsoft.Extensions.Compliance.Redaction;

namespace ProjectTemplate.Dependencies;

/// <summary>
/// Base class for presentation-layer handlers.
/// Provides access to the mediator, logger, redactor provider, and
/// presentation-layer-keyed dependency resolution.
/// </summary>
/// <remarks>
/// Inherit from this class in your presentation-layer handler partial classes.
/// Direct instantiation is not intended — the DI container creates instances.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class Presentation
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMediator _mediator;
    /// <summary>Logger scoped to the concrete handler type.</summary>
    protected readonly ILogger logger;
    /// <summary>Redactor provider used to mask PII before writing to logs.</summary>
    protected readonly IRedactorProvider redactorProvider;

    /// <summary>
    /// Inject any dependencies that are needed for the presentation layer.
    /// </summary>
    protected Presentation(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this._mediator = serviceProvider.GetRequiredService<IMediator>();
        this.logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        this.redactorProvider = serviceProvider.GetRequiredService<IRedactorProvider>();
    }

    /// <summary>Forwards the request to this feature's application layer. For use by generated boilerplate only.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected System.Threading.Tasks.Task<TResult> ForwardToApplicationLayerCore<TCommand, TResult>(
        TCommand command, System.Threading.CancellationToken cancellationToken)
        where TCommand : Cortex.Mediator.Commands.ICommand<TResult>
        => _mediator.SendCommandAsync<TCommand, TResult>(command, cancellationToken);

    /// <summary>Forwards a query to this feature's application layer. For use by generated boilerplate only.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected System.Threading.Tasks.Task<TResult> ForwardToApplicationLayerCoreQuery<TQuery, TResult>(
        TQuery query, System.Threading.CancellationToken cancellationToken)
        where TQuery : Cortex.Mediator.Queries.IQuery<TResult>
        => _mediator.SendQueryAsync<TQuery, TResult>(query, cancellationToken);

    /// <summary>Forwards a command directly to this feature's infrastructure layer. For use by advanced presentation orchestration only.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected System.Threading.Tasks.Task<TResult> ForwardToInfrastructureLayerCore<TCommand, TResult>(
        TCommand command, System.Threading.CancellationToken cancellationToken)
        where TCommand : Cortex.Mediator.Commands.ICommand<TResult>
        => _mediator.SendCommandAsync<TCommand, TResult>(command, cancellationToken);

    /// <summary>Forwards a query directly to this feature's infrastructure layer. For use by advanced presentation orchestration only.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected System.Threading.Tasks.Task<TResult> ForwardToInfrastructureLayerCoreQuery<TQuery, TResult>(
        TQuery query, System.Threading.CancellationToken cancellationToken)
        where TQuery : Cortex.Mediator.Queries.IQuery<TResult>
        => _mediator.SendQueryAsync<TQuery, TResult>(query, cancellationToken);

    /// <summary>Resolves a required presentation-layer-keyed service of type <typeparamref name="T"/>.</summary>
    protected T GetRequiredService<T>() where T : class
        => serviceProvider.GetRequiredPresentationDependency<T>();

    /// <summary>Resolves an optional presentation-layer-keyed service of type <typeparamref name="T"/>, or <c>null</c>.</summary>
    protected T? GetService<T>() where T : class
        => serviceProvider.GetPresentationDependency<T>();
}

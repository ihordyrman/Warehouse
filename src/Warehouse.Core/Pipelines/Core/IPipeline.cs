namespace Warehouse.Core.Pipelines.Core;

/// <summary>
///     Represents a sequence of processing steps in a workflow.
/// </summary>
/// <typeparam name="TContext">The type of context passed between steps.</typeparam>
public interface IPipeline<in TContext>
    where TContext : IPipelineContext
{
    IReadOnlyList<IPipelineStep<TContext>> Steps { get; }

    Task<PipelineResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}

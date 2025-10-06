namespace Warehouse.Core.Pipelines.Base;

public interface IPipeline<in TContext>
    where TContext : IPipelineContext
{
    IReadOnlyList<IPipelineStep<TContext>> Steps { get; }

    Task<PipelineResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}

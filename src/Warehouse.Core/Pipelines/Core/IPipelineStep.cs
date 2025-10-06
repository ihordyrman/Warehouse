namespace Warehouse.Core.Pipelines.Core;

public interface IPipelineStep<in TContext>
    where TContext : IPipelineContext
{
    int Order { get; }

    Task<PipelineStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    Task<bool> ShouldExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    Task OnErrorAsync(TContext context, Exception exception, CancellationToken cancellationToken = default);
}

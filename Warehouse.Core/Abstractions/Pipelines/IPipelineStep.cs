using Warehouse.Core.Models.Pipelines;

namespace Warehouse.Core.Abstractions.Pipelines;

public interface IPipelineStep<in TContext>
    where TContext : IPipelineContext
{
    int Order { get; }

    Task<PipelineStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    Task<bool> ShouldExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    Task OnErrorAsync(TContext context, Exception exception, CancellationToken cancellationToken = default);
}

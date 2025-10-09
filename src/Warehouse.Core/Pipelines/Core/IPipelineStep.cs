namespace Warehouse.Core.Pipelines.Core;

public interface IPipelineStep<in TContext>
    where TContext : IPipelineContext
{
    int Order { get; }

    PipelineStepType Type { get; }

    string Name { get; }

    Dictionary<string, string> Parameters { get; }

    Task<PipelineStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

    Task OnErrorAsync(TContext context, Exception exception, CancellationToken cancellationToken = default);
}

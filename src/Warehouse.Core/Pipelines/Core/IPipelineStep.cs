namespace Warehouse.Core.Pipelines.Core;

/// <summary>
///     A single unit of work in a pipeline.
/// </summary>
/// <typeparam name="TContext">The context type this step operates on.</typeparam>
public interface IPipelineStep<in TContext>
    where TContext : IPipelineContext
{
    int Order { get; }

    PipelineStepType Type { get; }

    string Name { get; }

    Dictionary<string, string> Parameters { get; }

    Task<PipelineStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}

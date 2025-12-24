namespace Warehouse.Core.Old.Pipelines.Core;

/// <summary>
///     Shared state accessible by all steps within a pipeline execution context.
/// </summary>
public interface IPipelineContext
{
    Guid ExecutionId { get; }

    DateTime StartedAt { get; }

    bool IsCancelled { get; set; }
}

namespace Warehouse.Core.Pipelines.Core;

public interface IPipelineContext
{
    Guid ExecutionId { get; }

    DateTime StartedAt { get; }

    bool IsCancelled { get; set; }
}

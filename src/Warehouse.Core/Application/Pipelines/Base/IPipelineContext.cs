namespace Warehouse.Core.Pipelines.Base;

public interface IPipelineContext
{
    Guid ExecutionId { get; }

    DateTime StartedAt { get; }

    bool IsCancelled { get; set; }
}

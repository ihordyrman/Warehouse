namespace Warehouse.Core.Abstractions.Pipelines;

public interface IPipelineContext
{
    Guid ExecutionId { get; }

    DateTime StartedAt { get; }

    bool IsCancelled { get; set; }
}

namespace Warehouse.Backend.Core.Abstractions.Pipelines;

public interface IPipelineContext
{
    Guid ExecutionId { get; }

    DateTime StartedAt { get; }

    bool IsCancelled { get; set; }
}

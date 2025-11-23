namespace Warehouse.Core.Pipelines.Core;

public interface IPipelineOrchestrator
{
    Task SynchronizePipelinesAsync();

    Task<bool> StartPipelineAsync(int pipelineId);

    Task<bool> StopPipelineAsync(int pipelineId);
}

namespace Warehouse.Core.Pipelines.Core;

public interface IPipelineExecutor
{
    int PipelineId { get; }

    bool IsRunning { get; }

    Task StartAsync(CancellationToken ct);

    Task StopAsync();
}

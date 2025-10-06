using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace Warehouse.Core.Application.Workers;

public interface IWorkerManager
{
    IReadOnlyDictionary<int, WorkerInstance> GetWorkers();

    WorkerInstance? GetWorker(int workerId);

    bool IsWorkerActive(int workerId);

    int GetWorkerCount();

    Task AddWorkerAsync(int workerId, WorkerInstance worker);

    Task RemoveWorkerAsync(int workerId);

    Task UpdateWorkerStatusAsync(int workerId, WorkerInstanceStatus instanceStatus);
}

public class WorkerManager(ILogger<WorkerManager> logger) : IWorkerManager
{
    private readonly ConcurrentDictionary<int, WorkerInstance> workers = new();

    public IReadOnlyDictionary<int, WorkerInstance> GetWorkers() => workers.ToImmutableDictionary();

    public WorkerInstance? GetWorker(int workerId) => workers.GetValueOrDefault(workerId);

    public bool IsWorkerActive(int workerId) => workers.ContainsKey(workerId);

    public int GetWorkerCount() => workers.Count;

    public Task AddWorkerAsync(int workerId, WorkerInstance worker)
    {
        if (!workers.TryAdd(workerId, worker))
        {
            logger.LogWarning("Worker {WorkerId} already exists - cannot add duplicate", workerId);
            throw new InvalidOperationException($"Worker {workerId} already exists");
        }

        logger.LogInformation("Worker {WorkerId} added to registry", workerId);
        return Task.CompletedTask;
    }

    public Task RemoveWorkerAsync(int workerId)
    {
        if (workers.TryRemove(workerId, out WorkerInstance? _))
        {
            logger.LogInformation("Worker {WorkerId} removed from registry", workerId);
        }
        else
        {
            logger.LogDebug("Worker {WorkerId} was not in registry during removal", workerId);
        }

        return Task.CompletedTask;
    }

    public Task UpdateWorkerStatusAsync(int workerId, WorkerInstanceStatus instanceStatus)
    {
        if (workers.TryGetValue(workerId, out WorkerInstance? worker))
        {
            worker.Status = instanceStatus;
            worker.LastStatusUpdate = DateTime.UtcNow;
            logger.LogDebug("Worker {WorkerId} status updated to {Status}", workerId, instanceStatus);
        }
        else
        {
            logger.LogWarning("Cannot update status for non-existent worker {WorkerId}", workerId);
        }

        return Task.CompletedTask;
    }
}

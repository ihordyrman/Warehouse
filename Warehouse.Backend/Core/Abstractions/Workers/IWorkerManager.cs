using Warehouse.Backend.Core.Application.Workers;

namespace Warehouse.Backend.Core.Abstractions.Workers;

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

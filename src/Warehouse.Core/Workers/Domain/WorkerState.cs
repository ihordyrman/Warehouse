namespace Warehouse.Core.Workers.Domain;

public enum WorkerState
{
    Running,
    Paused,
    Stopped,
    Error
}

using Warehouse.Backend.Core.Abstractions.Workers;
using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Application.Workers;

public class WorkerInstance
{
    public required IMarketWorker Worker { get; init; }

    public required WorkerConfiguration Configuration { get; init; }

    public required CancellationTokenSource CancellationTokenSource { get; init; }

    public required Task Task { get; init; }

    public required DateTime StartedAt { get; init; }

    public WorkerInstanceStatus Status { get; set; } = WorkerInstanceStatus.Starting;

    public bool IsHealthy { get; set; } = true;

    public string? LastError { get; set; }

    public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;

    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
}

public enum WorkerInstanceStatus
{
    Starting,
    Running,
    Stopping,
    Stopped,
    Error,
    Paused
}

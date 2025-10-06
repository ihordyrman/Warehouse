using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Workers.Domain;

public class WorkerStatus : AuditEntity
{
    public int Id { get; set; }

    public int WorkerId { get; set; }

    public MarketType MarketType { get; set; }

    public WorkerState State { get; set; }
}

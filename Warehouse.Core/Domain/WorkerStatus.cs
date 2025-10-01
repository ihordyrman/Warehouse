namespace Warehouse.Core.Domain;

public class WorkerStatus : AuditEntity
{
    public int Id { get; set; }

    public int WorkerId { get; set; }

    public MarketType MarketType { get; set; }

    public WorkerState State { get; set; }
}

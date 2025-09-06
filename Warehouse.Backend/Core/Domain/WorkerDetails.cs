namespace Warehouse.Backend.Core.Domain;

public class WorkerDetails : AuditEntity
{
    public int Id { get; set; }

    public bool Enabled { get; set; }

    public MarketType Type { get; set; }

    public string Symbol { get; set; } = string.Empty;
}

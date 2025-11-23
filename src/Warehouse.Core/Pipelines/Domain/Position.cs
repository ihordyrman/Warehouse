using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Pipelines.Domain;

public class Position : AuditEntity
{
    public int Id { get; set; }

    public int PipelineId { get; set; }

    public Pipeline Pipeline { get; set; } = null!;

    public string Symbol { get; set; } = string.Empty;

    public decimal EntryPrice { get; set; }

    public decimal Quantity { get; set; }

    public long? BuyOrderId { get; set; }

    public long? SellOrderId { get; set; }

    public PositionStatus Status { get; set; }

    public decimal? ExitPrice { get; set; }

    public DateTime? ClosedAt { get; set; }
}

public enum PositionStatus
{
    Open,
    Closed,
    Cancelled
}

using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Pipelines.Domain;

/// <summary>
///     Represents a trading position opened by a pipeline.
///     Tracks the entry, current state, and exit of a trade.
/// </summary>
public class Position : AuditEntity
{
    /// <summary>
    ///     Unique identifier for this position.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Foreign key to the Pipeline that opened this position.
    /// </summary>
    public int PipelineId { get; set; }

    /// <summary>
    ///     Navigation property to the owning Pipeline.
    /// </summary>
    public Pipeline Pipeline { get; set; } = null!;

    /// <summary>
    ///     The trading pair symbol (e.g., "BTC-USDT").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    ///     The price at which the position was opened (buy price).
    /// </summary>
    public decimal EntryPrice { get; set; }

    /// <summary>
    ///     The quantity of the base asset held in this position.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    ///     The exchange order ID for the buy order that opened this position.
    /// </summary>
    public long? BuyOrderId { get; set; }

    /// <summary>
    ///     The exchange order ID for the sell order that closed this position.
    ///     Null while position is still open.
    /// </summary>
    public long? SellOrderId { get; set; }

    /// <summary>
    ///     Current status of this position.
    /// </summary>
    public PositionStatus Status { get; set; }

    /// <summary>
    ///     The price at which the position was closed (sell price).
    ///     Null while position is still open.
    /// </summary>
    public decimal? ExitPrice { get; set; }

    /// <summary>
    ///     When the position was closed.
    ///     Null while position is still open.
    /// </summary>
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
///     Status of a trading position.
/// </summary>
public enum PositionStatus
{
    /// <summary>Position is currently active with unsold assets.</summary>
    Open,

    /// <summary>Position has been closed (sold).</summary>
    Closed,

    /// <summary>Position was cancelled before completion.</summary>
    Cancelled
}

using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Domain;

namespace Warehouse.Core.Orders.Models;

/// <summary>
///     Criteria for filtering order history.
/// </summary>
public class OrderHistoryFilter
{
    public int? PipelineId { get; init; }

    public MarketType? MarketType { get; init; }

    public string? Symbol { get; init; }

    public OrderStatus? Status { get; init; }

    public OrderSide? Side { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }
}

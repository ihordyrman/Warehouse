using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Domain;

namespace Warehouse.Core.Orders.Models;

public class OrderHistoryFilter
{
    public int? WorkerId { get; init; }

    public MarketType? MarketType { get; init; }

    public string? Symbol { get; init; }

    public OrderStatus? Status { get; init; }

    public OrderSide? Side { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }
}

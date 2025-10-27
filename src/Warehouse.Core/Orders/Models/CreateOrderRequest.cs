using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Domain;

namespace Warehouse.Core.Orders.Models;

public class CreateOrderRequest
{
    public int? WorkerId { get; init; }

    public required MarketType MarketType { get; init; }

    public required string Symbol { get; init; }

    public required OrderSide Side { get; init; }

    public required decimal Quantity { get; init; }

    public decimal? Price { get; init; }

    public decimal? StopPrice { get; init; }

    public decimal? TakeProfit { get; init; }

    public decimal? StopLoss { get; init; }

    public DateTime? ExpireTime { get; set; }
}

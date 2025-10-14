using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Domain;

namespace Warehouse.Core.Orders.Models;

public class CreateOrderRequest
{
    public int? WorkerId { get; init; }

    public required MarketType MarketType { get; init; }

    public required string Symbol { get; init; }

    public required OrderSide Side { get; init; }

    public required decimal Quantity { get; set; }

    public decimal? Price { get; set; }

    public decimal? StopPrice { get; set; }

    public decimal? TakeProfit { get; set; }

    public decimal? StopLoss { get; set; }

    public DateTime? ExpireTime { get; set; }
}

namespace Warehouse.Core.Orders.Models;

public class UpdateOrderRequest
{
    public decimal? Quantity { get; init; }

    public decimal? Price { get; init; }

    public decimal? StopPrice { get; init; }

    public decimal? TakeProfit { get; init; }

    public decimal? StopLoss { get; init; }
}

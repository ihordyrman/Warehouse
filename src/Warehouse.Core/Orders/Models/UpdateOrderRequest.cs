namespace Warehouse.Core.Orders.Models;

/// <summary>
///     Parameters for modifying an existing order.
///     Only non-null values will be updated.
/// </summary>
public class UpdateOrderRequest
{
    public decimal? Quantity { get; init; }

    public decimal? Price { get; init; }

    public decimal? StopPrice { get; init; }

    public decimal? TakeProfit { get; init; }

    public decimal? StopLoss { get; init; }
}

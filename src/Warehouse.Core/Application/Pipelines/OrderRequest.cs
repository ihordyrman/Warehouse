﻿namespace Warehouse.Core.Pipelines;

public class OrderRequest
{
    public required string Symbol { get; init; }

    public required OrderSide Side { get; init; }

    public required OrderType Type { get; init; }

    public required decimal Quantity { get; init; }

    public decimal? Price { get; init; }

    public decimal? StopLoss { get; init; }

    public decimal? TakeProfit { get; init; }

    public string? ClientOrderId { get; init; }
}

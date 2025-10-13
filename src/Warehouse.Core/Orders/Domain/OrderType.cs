namespace Warehouse.Core.Orders.Domain;

public enum OrderType
{
    Market,
    Limit,
    Stop,
    StopLimit,
    TrailingStop,
    TakeProfit,
    StopLoss
}

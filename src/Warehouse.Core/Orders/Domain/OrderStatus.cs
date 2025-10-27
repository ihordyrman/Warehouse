namespace Warehouse.Core.Orders.Domain;

public enum OrderStatus
{
    Pending,
    Placed,
    PartiallyFilled,
    Filled,
    Cancelled,
    Failed
}

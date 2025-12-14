namespace Warehouse.Core.Orders.Domain;

/// <summary>
///     Lifecycle status of a trade order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created locally but not yet sent to exchange.</summary>
    Pending = 0,

    /// <summary>Order submitted to exchange and waiting to be filled.</summary>
    Placed = 1,

    /// <summary>Order partially executed, waiting for remaining quantity.</summary>
    PartiallyFilled = 2,

    /// <summary>Order completely executed.</summary>
    Filled = 3,

    /// <summary>Order canceled before full execution.</summary>
    Cancelled = 4,

    /// <summary>Order failed due to an error (insufficient funds, invalid params, etc.).</summary>
    Failed = 5
}

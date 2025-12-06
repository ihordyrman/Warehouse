namespace Warehouse.Core.Orders.Domain;

/// <summary>
///     Lifecycle status of a trade order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created locally but not yet sent to exchange.</summary>
    Pending,

    /// <summary>Order submitted to exchange and waiting to be filled.</summary>
    Placed,

    /// <summary>Order partially executed, waiting for remaining quantity.</summary>
    PartiallyFilled,

    /// <summary>Order completely executed.</summary>
    Filled,

    /// <summary>Order cancelled before full execution.</summary>
    Cancelled,

    /// <summary>Order failed due to an error (insufficient funds, invalid params, etc.).</summary>
    Failed
}

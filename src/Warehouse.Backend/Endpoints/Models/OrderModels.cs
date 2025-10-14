using System.ComponentModel.DataAnnotations;
using Warehouse.Backend.Endpoints.Validation;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;

namespace Warehouse.Backend.Endpoints.Models;

public abstract class BaseOrderModel
{
    public long Id { get; set; }

    public int? WorkerId { get; set; }

    public MarketType MarketType { get; set; }

    public string ExchangeOrderId { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public OrderSide Side { get; set; }

    public OrderStatus Status { get; set; }

    public decimal Quantity { get; set; }

    public decimal? Price { get; set; }

    public decimal? StopPrice { get; set; }

    public decimal? Fee { get; set; }

    public DateTime? PlacedAt { get; set; }

    public DateTime? ExecutedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}

public class OrderResponse : BaseOrderModel
{
    public decimal? TakeProfit { get; set; }

    public decimal? StopLoss { get; set; }

    public static OrderResponse FromDomain(Order order)
        => new()
        {
            Id = order.Id,
            WorkerId = order.WorkerId,
            MarketType = order.MarketType,
            ExchangeOrderId = order.ExchangeOrderId,
            Symbol = order.Symbol,
            Side = order.Side,
            Status = order.Status,
            Quantity = order.Quantity,
            Price = order.Price,
            StopPrice = order.StopPrice,
            Fee = order.Fee,
            PlacedAt = order.PlacedAt,
            ExecutedAt = order.ExecutedAt,
            CancelledAt = order.CancelledAt,
            TakeProfit = order.TakeProfit,
            StopLoss = order.StopLoss
        };
}

public class CreateOrderApiRequest
{
    public int? WorkerId { get; set; }

    [Required(ErrorMessage = "Market type is required")]
    [ValidEnum(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType MarketType { get; set; }

    [Required(ErrorMessage = "Symbol is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Symbol must be between 3 and 20 characters")]
    public string Symbol { get; set; } = string.Empty;

    [Required(ErrorMessage = "Side is required")]
    [ValidEnum(typeof(OrderSide), ErrorMessage = "Invalid order side")]
    public OrderSide Side { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Stop price must be greater than 0")]
    public decimal? StopPrice { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Take profit must be greater than 0")]
    public decimal? TakeProfit { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Stop loss must be greater than 0")]
    public decimal? StopLoss { get; set; }

    public DateTime? ExpireTime { get; set; }

    public CreateOrderRequest ToServiceRequest()
        => new()
        {
            WorkerId = WorkerId,
            MarketType = MarketType,
            Symbol = Symbol.ToUpperInvariant(),
            Side = Side,
            Quantity = Quantity,
            Price = Price,
            StopPrice = StopPrice,
            TakeProfit = TakeProfit,
            StopLoss = StopLoss,
            ExpireTime = ExpireTime
        };
}

public class UpdateOrderApiRequest
{
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal? Quantity { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Stop price must be greater than 0")]
    public decimal? StopPrice { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Take profit must be greater than 0")]
    public decimal? TakeProfit { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Stop loss must be greater than 0")]
    public decimal? StopLoss { get; set; }

    public UpdateOrderRequest ToServiceRequest()
        => new()
        {
            Quantity = Quantity,
            Price = Price,
            StopPrice = StopPrice,
            TakeProfit = TakeProfit,
            StopLoss = StopLoss
        };
}

public class CancelOrderRequest
{
    [StringLength(200, ErrorMessage = "Reason must not exceed 200 characters")]
    public string? Reason { get; set; }
}

public class OrderHistoryFilterRequest
{
    public int? WorkerId { get; set; }

    [ValidEnum(typeof(MarketType), ErrorMessage = "Invalid market type")]
    public MarketType? MarketType { get; set; }

    [StringLength(20, ErrorMessage = "Symbol must not exceed 20 characters")]
    public string? Symbol { get; set; }

    [ValidEnum(typeof(OrderStatus), ErrorMessage = "Invalid order status")]
    public OrderStatus? Status { get; set; }

    [ValidEnum(typeof(OrderSide), ErrorMessage = "Invalid order side")]
    public OrderSide? Side { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Skip must be non-negative")]
    public int Skip { get; set; } = 0;

    [Range(1, 1000, ErrorMessage = "Take must be between 1 and 1000")]
    public int Take { get; set; } = 100;

    public OrderHistoryFilter ToServiceFilter()
        => new()
        {
            WorkerId = WorkerId,
            MarketType = MarketType,
            Symbol = Symbol?.ToUpperInvariant(),
            Status = Status,
            Side = Side,
            FromDate = FromDate,
            ToDate = ToDate
        };
}

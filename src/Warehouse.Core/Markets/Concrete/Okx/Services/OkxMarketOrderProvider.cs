using System.Globalization;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Markets.Concrete.Okx.Messages.Http;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Markets.Concrete.Okx.Services;

public class OkxMarketOrderProvider(OkxHttpService httpService, ILogger<OkxMarketOrderProvider> logger) : IMarketOrderProvider
{
    private const string TradeModeCash = "cash";
    private const string OrderTypeMarket = "market";
    private const string OrderTypeLimit = "limit";

    public MarketType MarketType => MarketType.Okx;

    public async Task<Result<string>> ExecuteOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        OkxPlaceOrderRequest request = MapToRequest(order);

        logger.LogInformation(
            "Placing {OrderType} {Side} order for {Symbol}: Qty={Quantity}, Price={Price}",
            request.OrderType,
            request.Side,
            request.InstrumentId,
            request.Size,
            request.Price ?? "market");

        Result<OkxPlaceOrderResponse> result = await httpService.PlaceOrderAsync(request);

        if (!result.IsSuccess)
        {
            logger.LogError("Failed to place order on OKX: {Error}", result.Error.Message);
            return Result<string>.Failure(result.Error);
        }

        OkxPlaceOrderResponse response = result.Value;

        if (!response.IsSuccess)
        {
            logger.LogError("OKX rejected order: {Code} - {Message}", response.StatusCode, response.StatusMessage);
            return Result<string>.Failure(new Error($"OKX order rejected: {response.StatusMessage}"));
        }

        logger.LogInformation(
            "Order placed successfully. ExchangeOrderId={OrderId}, ClientOrderId={ClientOrderId}",
            response.OrderId,
            response.ClientOrderId);

        return Result<string>.Success(response.OrderId);
    }

    private static OkxPlaceOrderRequest MapToRequest(Order order)
    {
        bool isLimitOrder = order.Price is > 0;

        return new OkxPlaceOrderRequest
        {
            InstrumentId = order.Symbol,
            TradeMode = TradeModeCash,
            Side = order.Side.ToOkxSide(),
            OrderType = isLimitOrder ? OrderTypeLimit : OrderTypeMarket,
            Size = order.Quantity.ToOkxDecimal(),
            Price = isLimitOrder ? order.Price!.Value.ToOkxDecimal() : null,
            ClientOrderId = order.Id.ToString(CultureInfo.InvariantCulture)
        };
    }
}

internal static class OkxOrderExtensions
{
    public static string ToOkxSide(this OrderSide side)
        => side switch
        {
            OrderSide.Buy => "buy",
            OrderSide.Sell => "sell",
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown order side")
        };

    public static string ToOkxDecimal(this decimal value) => value.ToString(CultureInfo.InvariantCulture);
}

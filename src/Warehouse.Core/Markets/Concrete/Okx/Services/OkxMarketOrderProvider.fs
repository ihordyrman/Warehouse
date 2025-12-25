namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System.Globalization
open Microsoft.Extensions.Logging
open Warehouse.Core.Markets.Concrete.Okx
open Warehouse.Core.Markets.Domain
open Warehouse.Core.Markets.Okx
open Warehouse.Core.Orders.Contracts
open Warehouse.Core.Orders.Domain
open Warehouse.Core.Shared.Errors

type OkxMarketOrderProvider(http: OkxHttp.T, logger: ILogger) =

    let toOkxSide (side: OrderSide) =
        match side with
        | OrderSide.Buy -> "buy"
        | OrderSide.Sell -> "sell"
        | _ -> failwith "unknown side"

    let mapToRequest (order: Order) =
        let isLimitOrder = order.Price.HasValue && order.Price.Value > 0m

        {
            InstrumentId = order.Symbol
            TradeMode = "cash"
            Side = toOkxSide order.Side
            OrderType = if isLimitOrder then "limit" else "market"
            Size = order.Quantity.ToString(CultureInfo.InvariantCulture)
            Price = if isLimitOrder then Some(order.Price.Value.ToString(CultureInfo.InvariantCulture)) else None
            ClientOrderId = Some(order.Id.ToString(CultureInfo.InvariantCulture))
            Tag = None
            ReduceOnly = None
            TargetCurrency = None
        }

    interface IMarketOrderProvider with
        member this.MarketType = MarketType.Okx

        member this.ExecuteOrderAsync(order, cancellationToken) =
            task {
                let request = mapToRequest order

                logger.LogInformation(
                    "Placing {OrderType} {Side} order for {Symbol}: Qty={Quantity}, Price={Price}",
                    request.OrderType,
                    request.Side,
                    request.InstrumentId,
                    request.Size,
                    (match request.Price with
                     | Some p -> p
                     | None -> "None")
                )

                let! result = http.placeOrder request

                match result with
                | Ok result ->
                    if not result.IsSuccess then
                        logger.LogError(
                            "OKX rejected order: {Code} - {Message}",
                            result.StatusCode,
                            result.StatusMessage
                        )

                        return Error(ServiceError.ApiError($"OKX order rejected: {result.StatusMessage}", None))
                    else
                        logger.LogInformation(
                            "Order placed successfully. ExchangeOrderId={OrderId}, ClientOrderId={ClientOrderId}",
                            result.OrderId,
                            result.ClientOrderId
                        )

                        return Ok(result.OrderId)
                | Error e ->
                    match e with
                    | ServiceError.ApiError(message, x) ->
                        logger.LogError("Error placing order: {Error}", message)
                        return Error(ServiceError.ApiError(message, x))
                    | _ ->
                        logger.LogError("Error placing order")
                        return Error(ServiceError.ApiError("Error placing order", None))
            }

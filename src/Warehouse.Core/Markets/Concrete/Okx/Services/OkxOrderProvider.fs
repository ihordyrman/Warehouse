namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System.Globalization
open Microsoft.Extensions.Logging
open Warehouse.Core.Markets.Concrete.Okx
open Warehouse.Core.Markets.Okx
open Warehouse.Core.Orders
open Warehouse.Core.Orders.Domain
open Warehouse.Core.Shared

module OkxOrderProvider =
    open Errors

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

    let create (http: OkxHttp.T) (logger: ILogger) : MarketOrderProvider.T =
        let executeOrder (order: Order) _ =
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
                | Ok response when response.IsSuccess ->
                    logger.LogInformation("Order placed: {OrderId}", response.OrderId)
                    return Ok response.OrderId
                | Ok response ->
                    logger.LogError("OKX rejected: {Message}", response.StatusMessage)
                    return Error(ApiError(response.StatusMessage, Some(int response.StatusCode)))
                | Error err -> return Error err
            }

        MarketOrderProvider.Okx executeOrder

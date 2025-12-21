namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System.Globalization
open Microsoft.Extensions.Logging
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Functional.Markets.Okx
open Warehouse.Core.Functional.Orders.Contracts
open Warehouse.Core.Functional.Orders.Domain
open Warehouse.Core.Functional.Shared

type OkxMarketOrderProvider(httpService: OkxHttpService, logger: ILogger<OkxMarketOrderProvider>) =

    let toOkxSide (side: OrderSide) =
        match side with
        | OrderSide.Buy -> "buy"
        | OrderSide.Sell -> "sell"
        | _ -> failwith "unknown side"

    let mapToRequest (order: Order) =
        let isLimitOrder = order.Price.HasValue && order.Price.Value > 0m

        { InstrumentId = order.Symbol
          TradeMode = "cash"
          Side = toOkxSide order.Side
          OrderType = if isLimitOrder then "limit" else "market"
          Size = order.Quantity.ToString(CultureInfo.InvariantCulture)
          Price = if isLimitOrder then Some(order.Price.Value.ToString(CultureInfo.InvariantCulture)) else None
          ClientOrderId = Some(order.Id.ToString(CultureInfo.InvariantCulture))
          Tag = None
          ReduceOnly = None
          TargetCurrency = None }

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
                     | None -> "market")
                )

                let! result = httpService.PlaceOrderAsync(request)

                if not result.IsSuccess then
                    logger.LogError("Failed to place order on OKX: {Error}", result.Error.Message)
                    return Result<string>.Failure(result.Error)
                else
                    let response = result.Value

                    if not response.IsSuccess then
                        logger.LogError("OKX rejected order: {Code} - {Message}", response.StatusCode, response.StatusMessage)
                        return Result<string>.Failure(Error($"OKX order rejected: {response.StatusMessage}"))
                    else
                        logger.LogInformation(
                            "Order placed successfully. ExchangeOrderId={OrderId}, ClientOrderId={ClientOrderId}",
                            response.OrderId,
                            response.ClientOrderId
                        )

                        return Result<string>.Success(response.OrderId)
            }

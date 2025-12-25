namespace Warehouse.Core.Pipelines

open Dapper.FSharp.PostgreSQL
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Data
open Warehouse.Core.Orders.Contracts
open Warehouse.Core.Orders.Domain
open Warehouse.Core.Pipelines.Domain
open Warehouse.Core.Pipelines.Parameters
open Warehouse.Core.Pipelines.Trading

module TradingSteps =
    open Steps

    let positionsTable = table'<Position> "positions"

    let checkPosition: StepDefinition<TradingContext> =
        let create (_: ValidatedParams) (services: IServiceProvider) : Step<TradingContext> =
            fun ctx ct ->
                task {
                    use scope = services.CreateScope()
                    use conn = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                    let! positions =
                        select {
                            for p in positionsTable do
                                where (p.PipelineId = ctx.PipelineId && p.Status = PositionStatus.Open)
                                take 1
                        }
                        |> conn.SelectAsync<Position>

                    match positions |> Seq.tryHead with
                    | Option.None -> return Continue({ ctx with Action = TradingAction.None }, "No open position")
                    | Some pos ->
                        let ctx' =
                            { ctx with
                                BuyPrice = Some pos.EntryPrice
                                Quantity = Some pos.Quantity
                                ActiveOrderId = Some pos.BuyOrderId
                                Action = TradingAction.Hold
                            }

                        return Continue(ctx', $"Position found - Entry: {pos.EntryPrice:F8}")
                }

        {
            Key = "check-position"
            Name = "Check Position"
            Description = "Checks if there is an open position for this pipeline."
            Category = StepCategory.Validation
            Icon = "fa-search-dollar"
            ParameterSchema = { Parameters = [] }
            Create = create
        }

    let executeTrade: StepDefinition<TradingContext> =
        let create (params': ValidatedParams) (services: IServiceProvider) : Step<TradingContext> =
            let tradeAmount = params' |> ValidatedParams.getDecimal "tradeAmount" 100m

            fun ctx ct ->
                task {
                    match ctx.Action with
                    | TradingAction.None
                    | TradingAction.Hold -> return Stop "No trade action required"
                    | TradingAction.Buy
                    | TradingAction.Sell ->
                        use scope = services.CreateScope()
                        let logger = scope.ServiceProvider.GetRequiredService<ILogger<_>>()
                        let orderManager = scope.ServiceProvider.GetRequiredService<IOrderManager>()

                        let side = if ctx.Action = TradingAction.Buy then OrderSide.Buy else OrderSide.Sell
                        let quantity = tradeAmount / ctx.CurrentPrice

                        let request =
                            {
                                Symbol = ctx.Symbol
                                Side = side
                                Quantity = quantity
                                Price = Option.None
                                StopPrice = Option.None
                                TakeProfit = Option.None
                                StopLoss = Option.None
                                ExpireTime = Option.None
                                MarketType = ctx.MarketType
                                PipelineId = Some ctx.PipelineId
                            }

                        let! result = orderManager.CreateOrderAsync(request, ct)

                        match result with
                        | Ok order ->
                            logger.LogInformation("Order placed: {Id}", order.Id)
                            return Continue(ctx, $"Order {order.Id} placed")
                        | Error err -> return Fail $"Order failed: {err}"
                }

        {
            Key = "execute-trade"
            Name = "Execute Trade"
            Description = "Executes buy or sell orders based on the current trading action."
            Category = StepCategory.Execution
            Icon = "fa-exchange-alt"
            ParameterSchema =
                {
                    Parameters =
                        [
                            {
                                Key = "tradeAmount"
                                Name = "Trade Amount (USDT)"
                                Description = "Amount in USDT to trade per order"
                                Type = Decimal(Some 1m, Some 100000m)
                                Required = true
                                DefaultValue = Some(DecimalValue 100m)
                                Group = Some "Order Settings"
                            }
                        ]
                }
            Create = create
        }

    let all = [ checkPosition; executeTrade ]

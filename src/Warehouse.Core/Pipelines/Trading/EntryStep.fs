namespace Warehouse.Core.Pipelines.Trading


open System
open Warehouse.Core.Markets.Services
open Warehouse.Core.Pipelines.Core.Parameters
open Warehouse.Core.Pipelines.Core.Steps
open Microsoft.Extensions.DependencyInjection
open Warehouse.Core.Domain
open Warehouse.Core.Pipelines.Trading

module EntryStep =
    let entryStep: StepDefinition<TradingContext> =
        let create (params': ValidatedParams) (services: IServiceProvider) : Step<TradingContext> =
            let tradeAmount = params' |> ValidatedParams.getDecimal "tradeAmount" 1000m

            fun ctx ct ->
                task {
                    match (ctx.ActiveOrderId, ctx.Action) with
                    | Option.None, TradingAction.None ->
                        use scope = services.CreateScope()
                        let orderManager = scope.ServiceProvider.GetRequiredService<OrdersManager.T>()

                        let request: CreateOrderRequest =
                            {
                                PipelineId = Some ctx.PipelineId
                                MarketType = ctx.MarketType
                                Symbol = ctx.Symbol
                                Side = OrderSide.Buy
                                Quantity = tradeAmount
                                Price = Option.None
                                StopPrice = Option.None
                                TakeProfit = Option.None
                                StopLoss = Option.None
                                ExpireTime = Option.None
                            }

                        let! result = orderManager.createOrder request ct

                        match result with
                        | Ok order ->
                            // temporary
                            let! _ = orderManager.executeOrder order.Id ct
                            let ctx' = { ctx with ActiveOrderId = Some(order.Id.ToString()) }

                            return
                                Continue(
                                    ctx',
                                    $"Created order {order.Id} for {tradeAmount:F8} {ctx.Symbol} at approx. {ctx.CurrentPrice:F2} USDT"
                                )
                        | Error err -> return Fail $"Order creation failed: {err}"
                    | _ -> return Continue(ctx, "Already have an active order or action in progress")
                }

        {
            Key = "entry-step"
            Name = "Entry Step"
            Description = "Executes the entry trade based on the defined trading action."
            Category = StepCategory.Execution
            Icon = "fa-sign-in-alt"
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

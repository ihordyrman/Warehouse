namespace Warehouse.Core.Pipelines.Trading

open System
open System.Threading
open Warehouse.Core.Infrastructure
open Warehouse.Core.Pipelines.Core.Parameters
open Warehouse.Core.Pipelines.Core.Steps
open Microsoft.Extensions.DependencyInjection
open Warehouse.Core.Domain
open Warehouse.Core.Pipelines.Trading
open Warehouse.Core.Repositories

module EntryStep =
    let private buy (services: IServiceProvider) (ctx: TradingContext) (ct: CancellationToken) : TradingContext =
        // order placement and execution logic goes here
        task {
            let orderRepo = services.GetRequiredService<OrderRepository.T>()
            let positionRepo = services.GetRequiredService<PositionRepository.T>()

            let! order =
                orderRepo.Create
                    {
                        PipelineId = ctx.PipelineId
                        Symbol = failwith "todo"
                        MarketType = failwith "todo"
                        Quantity = failwith "todo"
                        Side = failwith "todo"
                        Price = failwith "todo"
                    }

            let! result = Transaction.execute services (fun db txn -> task {

                return Ok ()

            })

            return { ctx with ActiveOrderId = Some order.Id }
        }



    let private sell (services: IServiceProvider) (ctx: TradingContext) (ct: CancellationToken) : TradingContext =
        // order placement and execution logic goes here
        failwith "Not implemented"

    let entryStep: StepDefinition<TradingContext> =
        let create (params': ValidatedParams) (services: IServiceProvider) : Step<TradingContext> =
            let tradeAmount = params' |> ValidatedParams.getDecimal "tradeAmount" 100m

            fun ctx ct ->
                task {
                    // order placement and execution logic goes here
                    match (ctx.ActiveOrderId, ctx.Action) with
                    | Option.None, TradingAction.Buy ->
                        let ctx = buy services ctx ct
                        return Continue(ctx, $"Placed buy order for {tradeAmount} USDT.")
                    | Some orderId, TradingAction.Sell ->
                        let ctx = sell services ctx ct
                        return Continue(ctx, $"Placed sell order for order ID {orderId}.")
                    | _ -> return Continue(ctx, "No action taken.")
                }

        {
            Key = "entry-step"
            Name = "Entry Step"
            Description = "Places an entry trade based on the defined strategy."
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

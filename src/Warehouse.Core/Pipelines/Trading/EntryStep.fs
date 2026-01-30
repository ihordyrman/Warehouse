namespace Warehouse.Core.Pipelines.Trading

open System
open Warehouse.Core.Pipelines.Core.Parameters
open Warehouse.Core.Pipelines.Core.Steps
open Microsoft.Extensions.DependencyInjection
open Warehouse.Core.Domain
open Warehouse.Core.Pipelines.Trading
open Warehouse.Core.Repositories

/// Initial step to create an entry order based on the trading action.
module EntryStep =
    let entryStep: StepDefinition<TradingContext> =
        let create (params': ValidatedParams) (services: IServiceProvider) : Step<TradingContext> =
            let tradeAmount = params' |> ValidatedParams.getDecimal "tradeAmount" 1000m

            fun ctx ct ->
                task {
                    match (ctx.ActiveOrderId, ctx.Action) with
                    | Option.None, TradingAction.None ->
                        use scope = services.CreateScope()
                        let repo = scope.ServiceProvider.GetRequiredService<PositionRepository.T>()
                        let! position = repo.GetOpen ctx.PipelineId ct

                        match position with
                        | Error err -> return Stop $"Error retrieving position: {err}"
                        | Ok position ->
                            match position with
                            | Some position when position.Status = PositionStatus.Open ->
                                return
                                    Continue(
                                        { ctx with
                                            ActiveOrderId = Some position.BuyOrderId
                                            Action = TradingAction.Hold
                                        },
                                        "Open position exists, setting action to Hold"
                                    )
                            | _ ->
                                return
                                    Continue(
                                        // do I really need to set Quantity here?
                                        { ctx with Action = TradingAction.None; Quantity = Some tradeAmount },
                                        $"No active orders or positions, ready to place entry order for {tradeAmount} USDT"
                                    )
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

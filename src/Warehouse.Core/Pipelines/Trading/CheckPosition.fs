namespace Warehouse.Core.Pipelines.Trading

open System
open Warehouse.Core.Pipelines.Core.Parameters
open Warehouse.Core.Pipelines.Core.Steps
open Microsoft.Extensions.DependencyInjection
open Warehouse.Core.Domain
open Warehouse.Core.Pipelines.Trading
open Warehouse.Core.Repositories

module CheckPosition =
    let checkPosition: StepDefinition<TradingContext> =
        let create (_: ValidatedParams) (services: IServiceProvider) : Step<TradingContext> =
            fun ctx ct ->
                task {
                    use scope = services.CreateScope()
                    let repo = scope.ServiceProvider.GetRequiredService<PositionRepository.T>()
                    let! position = repo.GetOpen ctx.PipelineId ct

                    match position with
                    | Error err -> return Fail $"Error retrieving position: {err}"
                    | Ok position ->
                        match position with
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

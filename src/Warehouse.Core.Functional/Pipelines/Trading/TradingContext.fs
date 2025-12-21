namespace Warehouse.Core.Functional.Pipelines.Trading

open System
open Warehouse.Core.Functional.Markets.Contracts
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Functional.Pipelines.Contracts

type TradingContext() =
    interface IPipelineContext with
        member this.ExecutionId = this.ExecutionId
        member this.StartedAt = this.StartedAt
        member this.IsCancelled = this.IsCancelled

        member this.IsCancelled
            with set value = this.IsCancelled <- value

    member val PipelineId = 0 with get, set
    member val CurrentMarketData: MarketData option = None with get, set
    member val MarketType = Unchecked.defaultof<MarketType> with get, set
    member val Symbol = "" with get, set
    member val BuyPrice: decimal option = None with get, set
    member val Quantity: decimal option = None with get, set
    member val Action = TradingAction.None with get, set
    member val ActiveOrderId: int64 option = None with get, set
    member val CurrentPrice = 0m with get, set
    member val ExecutionId = Guid.CreateVersion7() with get
    member val StartedAt = DateTime.UtcNow with get
    member val IsCancelled = false with get, set

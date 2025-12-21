namespace Warehouse.Core.Functional.Pipelines.Domain

open System
open System.Collections.Generic
open Warehouse.Core.Functional.Shared.Domain
open Warehouse.Core.Functional.Markets.Domain

type PipelineStatus =
    | Idle = 0
    | Running = 1
    | Paused = 2
    | Error = 3

type PositionStatus =
    | Open = 0
    | Closed = 1
    | Cancelled = 2

[<AllowNullLiteral>]
type Pipeline() =
    inherit AuditEntity()

    member val Id: int = 0 with get, set
    member val Name: string = String.Empty with get, set
    member val Symbol: string = String.Empty with get, set
    member val MarketType: MarketType = MarketType.Okx with get, set
    member val Enabled: bool = false with get, set
    member val ExecutionInterval: TimeSpan = TimeSpan.Zero with get, set
    member val LastExecutedAt: Nullable<DateTime> = Nullable() with get, set
    member val Status: PipelineStatus = PipelineStatus.Idle with get, set
    member val Steps: List<PipelineStep> = List<PipelineStep>() with get, set
    member val Tags: List<string> = List<string>() with get, set

and [<AllowNullLiteral>] PipelineStep() =
    inherit AuditEntity()

    member val Id: int = 0 with get, set
    member val PipelineDetailsId: int = 0 with get, set
    member val Pipeline: Pipeline = null with get, set
    member val StepTypeKey: string = String.Empty with get, set
    member val Name: string = String.Empty with get, set
    member val Order: int = 0 with get, set
    member val IsEnabled: bool = false with get, set
    member val Parameters: Dictionary<string, string> = Dictionary<string, string>() with get, set

[<AllowNullLiteral>]
type Position() =
    inherit AuditEntity()

    member val Id: int = 0 with get, set
    member val PipelineId: int = 0 with get, set
    member val Pipeline: Pipeline = null with get, set
    member val Symbol: string = String.Empty with get, set
    member val EntryPrice: decimal = 0m with get, set
    member val Quantity: decimal = 0m with get, set
    member val BuyOrderId: Nullable<int64> = Nullable() with get, set
    member val SellOrderId: Nullable<int64> = Nullable() with get, set
    member val Status: PositionStatus = PositionStatus.Open with get, set
    member val ExitPrice: Nullable<decimal> = Nullable() with get, set
    member val ClosedAt: Nullable<DateTime> = Nullable() with get, set

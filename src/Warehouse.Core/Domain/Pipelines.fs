namespace Warehouse.Core.Pipelines.Domain

open System
open System.Collections.Generic
open Warehouse.Core.Markets.Domain

type PipelineStatus =
    | Idle = 0
    | Running = 1
    | Paused = 2
    | Error = 3

type PositionStatus =
    | Open = 0
    | Closed = 1
    | Cancelled = 2

type Pipeline =
    {
        Id: int
        Name: string
        Symbol: string
        MarketType: MarketType
        Enabled: bool
        ExecutionInterval: TimeSpan
        LastExecutedAt: Nullable<DateTime>
        Status: PipelineStatus
        Steps: PipelineStep list
        Tags: string list
        CreatedAt: DateTime
        UpdatedAt: DateTime
    }

and PipelineStep =
    {
        Id: int
        PipelineDetailsId: int
        Pipeline: Pipeline
        StepTypeKey: string
        Name: string
        Order: int
        IsEnabled: bool
        Parameters: Dictionary<string, string>
        CreatedAt: DateTime
        UpdatedAt: DateTime
    }

type Position =
    {
        Id: int
        PipelineId: int
        Pipeline: Pipeline
        Symbol: string
        EntryPrice: decimal
        Quantity: decimal
        BuyOrderId: string
        SellOrderId: string
        Status: PositionStatus
        ExitPrice: Nullable<decimal>
        ClosedAt: Nullable<DateTime>
        CreatedAt: DateTime
        UpdatedAt: DateTime
    }

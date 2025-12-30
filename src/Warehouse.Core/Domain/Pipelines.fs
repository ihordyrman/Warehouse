namespace Warehouse.Core.Domain

open System
open System.Collections.Generic
open System.Runtime.Serialization

type PipelineStatus =
    | Idle = 0
    | Running = 1
    | Paused = 2
    | Error = 3

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

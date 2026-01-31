namespace Warehouse.Core.Pipelines.Trading

open Warehouse.Core.Pipelines.Trading

module TradingSteps =
    open CheckPosition
    open PositionGateStep
    open EntryStep

    let all = [ entryStep; checkPosition; positionGateStep ]

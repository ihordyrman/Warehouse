namespace Warehouse.Core.Pipelines.Trading

module TradingSteps =
    open CheckPosition
    open EntryStep

    let all = [ checkPosition; entryStep ]

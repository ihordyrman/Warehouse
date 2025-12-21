namespace Warehouse.Core.Functional.Pipelines.Trading

type TradingAction =
    | None = 0
    | Buy = 1
    | Sell = 2
    | Hold = 3

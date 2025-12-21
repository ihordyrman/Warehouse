namespace Warehouse.Core.Functional.Shared.Domain

open System

[<AbstractClass; AllowNullLiteral>]
type AuditEntity() =
    member val CreatedAt: DateTime = DateTime.MinValue with get, set
    member val UpdatedAt: DateTime = DateTime.MinValue with get, set

[<CLIMutable>]
type Candlestick =
    { Id: int64
      Symbol: string
      MarketType: int
      Timestamp: DateTime
      Open: decimal
      High: decimal
      Low: decimal
      Close: decimal
      Volume: decimal
      VolumeQuote: decimal
      IsCompleted: bool
      Timeframe: string }

type Instrument =
    | USDT = 0
    | BTC = 1
    | OKB = 2
    | SOL = 3
    | ETH = 4
    | DOGE = 5
    | XRP = 6
    | BCH = 7
    | LTC = 8

[<Struct>]
type Pair =
    { Left: Instrument
      Right: Instrument }

    override this.ToString() = $"{this.Left}-{this.Right}"

namespace Warehouse.Core.Functional.Markets.Domain

open System
open Warehouse.Core.Functional.Shared.Domain

type MarketType =
    | Okx = 0
    | Binance = 1

type ConnectionState =
    | Disconnected = 0
    | Connecting = 1
    | Connected = 2
    | Failed = 3

[<AllowNullLiteral>]
type Market() =
    inherit AuditEntity()

    member val Id: int = 0 with get, set
    member val Type: MarketType = MarketType.Okx with get, set
    member val Credentials: MarketCredentials = null with get, set

and [<AllowNullLiteral>] MarketCredentials() =
    inherit AuditEntity()

    member val Id: int = 0 with get, set
    member val MarketId: int = 0 with get, set
    member val Market: Market = null with get, set
    member val ApiKey: string = String.Empty with get, set
    member val Passphrase: string = String.Empty with get, set
    member val SecretKey: string = String.Empty with get, set
    member val IsSandbox: bool = false with get, set

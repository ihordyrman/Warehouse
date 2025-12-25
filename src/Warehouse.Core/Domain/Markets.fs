namespace Warehouse.Core.Markets.Domain

open System

type MarketType =
    | Okx = 0
    | Binance = 1

type ConnectionState =
    | Disconnected = 0
    | Connecting = 1
    | Connected = 2
    | Failed = 3

type Market =
    {
        Id: int
        Type: MarketType
        Credentials: MarketCredentials
        CreatedAt: DateTime
        UpdatedAt: DateTime
    }

and MarketCredentials =
    {
        Id: int
        MarketId: int
        Market: Market
        ApiKey: string
        Passphrase: string
        SecretKey: string
        IsSandbox: bool
        CreatedAt: DateTime
        UpdatedAt: DateTime
    }

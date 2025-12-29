namespace Warehouse.Core.Markets

open System
open System.Collections.Frozen
open Warehouse.Core.Domain

[<CLIMutable>]
type Balance =
    {
        Currency: string
        Available: decimal
        Total: decimal
        Frozen: decimal
        InOrder: decimal
        MarketType: MarketType
        UpdatedAt: DateTime
    }

[<CLIMutable>]
type AccountBalance =
    {
        MarketType: MarketType
        TotalEquity: decimal
        AvailableBalance: decimal
        UsedMargin: decimal
        UnrealizedPnl: decimal
        Balances: Balance list
        UpdatedAt: DateTime
    }

[<CLIMutable>]
type BalanceSnapshot =
    {
        MarketType: MarketType
        Spot: Map<string, Balance>
        Funding: Map<string, Balance>
        mutable AccountSummary: AccountBalance option
        Timestamp: DateTime
    }

type MarketData =
    {
        Asks: FrozenDictionary<decimal, struct (decimal * int)>
        Bids: FrozenDictionary<decimal, struct (decimal * int)>
    }

type MarketDataEvent = { Symbol: string; Source: MarketType; Asks: string[][]; Bids: string[][] }

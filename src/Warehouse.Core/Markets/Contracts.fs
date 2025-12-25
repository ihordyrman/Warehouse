namespace Warehouse.Core.Markets.Contracts

open System
open System.Collections.Generic
open System.Collections.Frozen
open System.Threading
open System.Threading.Tasks
open Warehouse.Core.Markets.Domain
open Warehouse.Core.Shared.Domain
open Warehouse.Core.Shared.Errors

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

type IMarketAdapter =
    abstract member MarketType: MarketType with get
    abstract member ConnectionState: ConnectionState with get
    abstract member ConnectAsync: ct: CancellationToken -> Task<bool>
    abstract member DisconnectAsync: ct: CancellationToken -> Task
    abstract member SubscribeAsync: symbol: string * ct: CancellationToken -> Task
    abstract member UnsubscribeAsync: symbol: string * ct: CancellationToken -> Task

type IMarketDataCache =
    abstract member GetData: symbol: string * marketType: MarketType -> MarketData option
    abstract member Update: marketDataEvent: MarketDataEvent -> unit

type ICandlestickService =
    abstract member SaveCandlesticksAsync:
        candlesticks: IEnumerable<Candlestick> * cancellationToken: CancellationToken -> Task<int>

    abstract member GetCandlesticksAsync:
        symbol: string *
        marketType: MarketType *
        timeframe: string *
        fromDate: Nullable<DateTime> *
        toDate: Nullable<DateTime> *
        limit: Nullable<int> ->
            IAsyncEnumerable<Candlestick>

    abstract member GetLatestCandlestickAsync:
        symbol: string * marketType: MarketType * timeframe: string * cancellationToken: CancellationToken ->
            Task<Candlestick option>

namespace Warehouse.Core

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Warehouse.Core
open Warehouse.Core.Markets
open Warehouse.Core.Markets.Concrete
open Warehouse.Core.Markets.Okx.Constants
open Warehouse.Core.Markets.Okx
open Warehouse.Core.Domain
open FSharp.Control
open Warehouse.Core.Shared

type SyncConfig =
    {
        Timeframe: string
        BatchSize: int
        Symbols: Instrument[]
        SyncStartDate: DateTime
        Interval: TimeSpan
    }

type SyncDependencies = { OkxHttp: OkxHttp.T; CandlestickStore: CandlestickStore.T; Logger: ILogger }

module CandlestickSync =
    open Errors

    let toPairSymbol (instrument: Instrument) = { Left = instrument; Right = Instrument.USDT }.ToString()

    let toCandlestick (symbol: string) (timeframe: string) (okxCandle: OkxCandlestick) : Candlestick =
        {
            Id = 0L
            Symbol = symbol
            MarketType = int MarketType.Okx
            Timestamp = okxCandle.Timestamp
            Open = okxCandle.Open
            High = okxCandle.High
            Low = okxCandle.Low
            Close = okxCandle.Close
            Volume = okxCandle.Volume
            VolumeQuote = okxCandle.VolumeQuoteCurrency
            IsCompleted = okxCandle.IsCompleted
            Timeframe = timeframe
        }

    let fetchBatch (http: OkxHttp.T) (logger: ILogger) (symbol: string) (config: SyncConfig) (fromDate: DateTime) =
        task {
            let before = DateTimeOffset(fromDate).ToUnixTimeMilliseconds().ToString()

            let! result =
                http.getCandlesticks
                    symbol
                    { Bar = Some config.Timeframe; Before = Some before; After = None; Limit = Some config.BatchSize }

            return
                match result with
                | Ok candles ->
                    logger.LogDebug("Fetched {Count} candlesticks for {Symbol}", candles.Length, symbol)
                    candles |> Array.map (toCandlestick symbol config.Timeframe) |> Ok
                | Error err ->
                    logger.LogError("Failed to fetch candlesticks: {Error}", serviceMessage err)
                    Error err
        }

    let syncSymbol (deps: SyncDependencies) (config: SyncConfig) (instrument: Instrument) =
        task {
            let symbol = toPairSymbol instrument
            let! latestCandle = deps.CandlestickStore.GetLatest symbol MarketType.Okx config.Timeframe

            let startFrom =
                match latestCandle with
                | Some c -> c.Timestamp.AddMinutes -2.0
                | None -> config.SyncStartDate

            let! result = fetchBatch deps.OkxHttp deps.Logger symbol config startFrom

            return
                match result with
                | Ok candles when candles.Length > 0 -> Some(candles |> Array.toList)
                | Ok _ -> None
                | Error _ -> None
        }

    let runSyncCycle (deps: SyncDependencies) (config: SyncConfig) =
        task {
            let! results = config.Symbols |> Array.map (syncSymbol deps config) |> Task.WhenAll

            let allCandles = results |> Array.choose id |> List.concat

            if not allCandles.IsEmpty then
                let! saved = deps.CandlestickStore.Save allCandles
                let latestTs = allCandles |> List.map _.Timestamp |> List.max
                deps.Logger.LogInformation("Saved {Count} candlesticks. Latest: {Timestamp}", saved, latestTs)

            return allCandles.Length
        }

module SyncDependencies =
    let create (scope: IServiceScope) (logger: ILogger) : SyncDependencies =
        {
            OkxHttp = CompositionRoot.createOkxHttp scope.ServiceProvider
            CandlestickStore = CompositionRoot.createCandlestickStore scope.ServiceProvider
            Logger = logger
        }

type OkxSynchronizationWorker(scopeFactory: IServiceScopeFactory, logger: ILogger<OkxSynchronizationWorker>) =
    inherit BackgroundService()

    let config =
        {
            Timeframe = CandlestickTimeframes.OneMinute
            BatchSize = 100
            Symbols =
                [|
                    Instrument.OKB
                    Instrument.BTC
                    Instrument.SOL
                    Instrument.ETH
                    Instrument.DOGE
                    Instrument.XRP
                    Instrument.BCH
                    Instrument.LTC
                |]
            SyncStartDate = DateTime.Today
            Interval = TimeSpan.FromMinutes 1.0
        }

    override _.ExecuteAsync(ct: CancellationToken) =
        task {
            use timer = new PeriodicTimer(config.Interval)

            while not ct.IsCancellationRequested do
                let! tick = timer.WaitForNextTickAsync(ct)

                if tick then
                    use scope = scopeFactory.CreateScope()
                    let deps = SyncDependencies.create scope logger

                    try
                        let! _ = CandlestickSync.runSyncCycle deps config
                        ()
                    with ex ->
                        logger.LogError(ex, "Error during sync cycle")
        }

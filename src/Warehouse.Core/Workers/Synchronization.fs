namespace Warehouse.Core.Workers

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open FSharp.Control
open Warehouse.Core.Markets.Exchanges.Okx
open Warehouse.Core.Repositories
open Warehouse.Core.Shared

type SyncConfig =
    {
        Timeframe: string
        BatchSize: int
        Symbols: Instrument[]
        SyncStartDate: DateTime
        Interval: TimeSpan
    }

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

    let fetchBatch (http: Http.T) (logger: ILogger) (symbol: string) (config: SyncConfig) (fromDate: DateTime) =
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

    let syncSymbol
        (repo: CandlestickRepository.T)
        (http: Http.T)
        (logger: ILogger)
        (config: SyncConfig)
        (instrument: Instrument)
        (ct: CancellationToken)
        =
        async {
            let symbol = toPairSymbol instrument
            let! latestCandle = repo.GetLatest symbol MarketType.Okx config.Timeframe ct |> Async.AwaitTask

            match latestCandle with
            | Error err ->
                logger.LogError("Failed to get latest candlestick for {Symbol}: {Error}", symbol, serviceMessage err)
                return None
            | Ok latestCandle ->
                let startFrom =
                    match latestCandle with
                    | Some c -> c.Timestamp.AddMinutes -2.0
                    | None -> config.SyncStartDate

                let! result = fetchBatch http logger symbol config startFrom |> Async.AwaitTask

                return
                    match result with
                    | Ok candles when candles.Length > 0 -> Some(candles |> Array.toList)
                    | Ok _ -> None
                    | Error _ -> None
        }

    let runSyncCycle (scopeFactory: IServiceScopeFactory) (config: SyncConfig) (ct: CancellationToken) =
        async {
            let scope = scopeFactory.CreateScope()
            let repo = scope.ServiceProvider.GetRequiredService<CandlestickRepository.T>()
            let http = scope.ServiceProvider.GetRequiredService<Http.T>()

            let logger =
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OkxCandlestickSync")

            let! results =
                config.Symbols
                |> Array.map (syncSymbol repo http logger config)
                |> Array.map (fun x -> x ct)
                |> Async.Sequential

            let allCandles = results |> Array.choose id |> List.concat

            if allCandles.Length > 0 then
                let! saved = repo.Save allCandles ct |> Async.AwaitTask

                match saved with
                | Error err -> logger.LogError("Failed to save candlesticks: {Error}", serviceMessage err)
                | Ok saved ->
                    let latestTs = allCandles |> List.map _.Timestamp |> List.max
                    logger.LogInformation("Saved {Count} candlesticks. Latest: {Timestamp}", saved, latestTs)

            return allCandles.Length
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
                    try
                        let! _ = CandlestickSync.runSyncCycle scopeFactory config ct
                        ()
                    with ex ->
                        logger.LogError(ex, "Error during sync cycle")
        }

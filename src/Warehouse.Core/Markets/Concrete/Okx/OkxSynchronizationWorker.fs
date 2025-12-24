namespace Warehouse.Core.Markets.Concrete.Okx

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Warehouse.Core.Functional.Shared.Domain
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Functional.Markets.Contracts
open Warehouse.Core.Markets.Concrete.Okx.Services
open FSharp.Control

type OkxSynchronizationWorker(scopeFactory: IServiceScopeFactory, logger: ILogger<OkxSynchronizationWorker>) =
    inherit BackgroundService()

    let timeframe = "1m" // CandlestickTimeframes.OneMinute
    let batchSize = 100

    let symbols =
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

    let syncStartDate = DateTime.Today
    let periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(1.0))

    let getCandlesticksAsync (symbol: string) (fromDate: DateTime) (okxHttpService: OkxHttpService) =
        taskSeq {
            logger.LogInformation("Starting candlestick sync for {Symbol} from {FromDate}", symbol, fromDate)

            let before = DateTimeOffset(fromDate).ToUnixTimeMilliseconds().ToString()

            let! result =
                okxHttpService.GetCandlesticksAsync(symbol, bar = timeframe, before = before, limit = batchSize)
                |> Async.AwaitTask

            match result with
            | Ok result ->
                let okxCandles = result

                if okxCandles.Length = 0 then
                    logger.LogInformation("No more candlesticks to fetch")
                    ()
                else
                    logger.LogDebug("Fetched {Count} candlesticks", okxCandles.Length)

                    for okxCandle in okxCandles do
                        yield
                            {
                                Id = 0
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
            | Error error -> logger.LogError("Failed to fetch candlesticks: {Error}", error)
        }

    override this.ExecuteAsync(stoppingToken: CancellationToken) =
        task {
            let candlesticks = ResizeArray<Candlestick>()

            while not stoppingToken.IsCancellationRequested do
                let! tick = periodicTimer.WaitForNextTickAsync(stoppingToken)

                if tick then
                    use scope = scopeFactory.CreateAsyncScope()
                    let candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>()
                    let okxHttpService = scope.ServiceProvider.GetRequiredService<OkxHttpService>()

                    for symbol in symbols do
                        let pairSymbol = { Left = symbol; Right = Instrument.USDT }.ToString()

                        let! latestCandle =
                            candlestickService.GetLatestCandlestickAsync(
                                pairSymbol,
                                MarketType.Okx,
                                timeframe,
                                stoppingToken
                            )

                        let startFrom =
                            match latestCandle with
                            | Some c -> c.Timestamp.AddMinutes(-2.0)
                            | None -> syncStartDate

                        // Converting AsyncSeq to Task/Loop
                        // System.Threading.Tasks.Extensions or similar providing AsyncEnumerable?
                        // For simplicity using loop if GetCandlesticksAsync was enumerable
                        // But I defined it as asyncSeq. F# Control.AsyncSeq is good but might need library.
                        // Let's rewrite `getCandlesticksAsync` to return Task<seq<Candlestick>> or similar to avoid extra dependencies if AsyncSeq not present. Use simple recursion or loop inside task.

                        // Re-implementing inner logic inline or via simple helper strictly Task based.

                        let rec fetchBatch fromDate =
                            task {
                                let before = DateTimeOffset(fromDate).ToUnixTimeMilliseconds().ToString()

                                let! result =
                                    okxHttpService.GetCandlesticksAsync(
                                        pairSymbol,
                                        bar = timeframe,
                                        before = before,
                                        limit = batchSize
                                    )

                                match result with
                                | Error error ->
                                    // logger.LogError("Failed to fetch candlesticks: {Error}", error.Message)
                                    return [||]
                                | Ok okxCandles -> return okxCandles
                            }

                        // Just one batch for now per cycle per symbol?
                        // The original C# code does `await foreach` so it might fetch multiple batches?
                        // "GetCandlesticksAsync" in C# yields items from *one* result of "GetCandlesticksAsync".
                        // Ah, the C# GetCandlesticksAsync method calls okxHttpService.GetCandlesticksAsync ONCE.
                        // So it is NOT a recursive historical fetcher in the loop. It just fetches one batch.
                        // Wait, `await foreach (Candlestick candlestick in GetCandlesticksAsync(...))` inside the loop.
                        // And `GetCandlesticksAsync` calls `okxHttpService.GetCandlesticksAsync` ONCE.
                        // So it is fundamentally fetching ONE batch of 100 candles every minute?
                        // Yes, looks like a synchronization of RECENT candles, not full history.

                        let! batch = fetchBatch startFrom

                        if batch.Length > 0 then
                            logger.LogDebug("Fetched {Count} candlesticks for {Symbol}", batch.Length, pairSymbol)

                            for okxCandle in batch do
                                candlesticks.Add(
                                    {
                                        Id = 0
                                        Symbol = pairSymbol
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
                                )

                    if candlesticks.Count > 0 then
                        let! saved = candlestickService.SaveCandlesticksAsync(candlesticks, stoppingToken)

                        logger.LogInformation(
                            "Saved batch of {Count} candlesticks. Latest: {LatestTimestamp}.",
                            saved,
                            candlesticks |> Seq.map _.Timestamp |> Seq.max
                        )

                        candlesticks.Clear()
        }

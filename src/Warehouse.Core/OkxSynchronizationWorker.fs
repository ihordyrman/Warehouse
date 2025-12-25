namespace Warehouse.Core

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Warehouse.Core
open Warehouse.Core.Markets.Concrete.Okx
open Warehouse.Core.Markets.Concrete.Okx.Constants
open Warehouse.Core.Shared.Errors
open Warehouse.Core.Shared.Domain
open Warehouse.Core.Markets.Domain
open FSharp.Control

type OkxSynchronizationWorker(scopeFactory: IServiceScopeFactory, logger: ILogger<OkxSynchronizationWorker>) =
    inherit BackgroundService()

    let timeframe = CandlestickTimeframes.OneMinute
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

    let getCandlesticksAsync (symbol: string) (fromDate: DateTime) (okxHttp: OkxHttp.T) =
        taskSeq {
            logger.LogInformation("Starting candlestick sync for {Symbol} from {FromDate}", symbol, fromDate)

            let before = DateTimeOffset(fromDate).ToUnixTimeMilliseconds().ToString()

            let! result =
                okxHttp.getCandlesticks symbol { Bar = Some timeframe; Before = Some before; After = None; Limit = Some batchSize }
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
                    let candlestickStore = CompositionRoot.createCandlestickStore scope.ServiceProvider
                    let http = CompositionRoot.createOkxHttp scope.ServiceProvider

                    for symbol in symbols do
                        let pairSymbol = { Left = symbol; Right = Instrument.USDT }.ToString()

                        let! latestCandle = candlestickStore.GetLatest pairSymbol MarketType.Okx timeframe

                        let startFrom =
                            match latestCandle with
                            | Some c -> c.Timestamp.AddMinutes -2.0
                            | None -> syncStartDate

                        let rec fetchBatch fromDate =
                            task {
                                let before = DateTimeOffset(fromDate).ToUnixTimeMilliseconds().ToString()

                                let! result = http.getCandlesticks pairSymbol { Bar = Some timeframe; Before = Some before; After = None; Limit = Some batchSize }

                                match result with
                                | Ok okxCandles -> return okxCandles
                                | Error error ->
                                    logger.LogError("Failed to fetch candlesticks: {Error}", serviceMessage error)
                                    return [||]
                            }

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
                        let! saved = candlestickStore.Save(candlesticks |> Seq.toList)

                        logger.LogInformation("Saved batch of {Count} candlesticks. Latest: {LatestTimestamp}.", saved, candlesticks |> Seq.map _.Timestamp |> Seq.max)

                        candlesticks.Clear()
        }

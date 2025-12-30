namespace Warehouse.Core.Markets.Stores

open System
open System.Data
open System.Threading.Tasks
open Dapper.FSharp.PostgreSQL
open Warehouse.Core.Domain

module CandlestickStore =

    let private candlesticks = table'<Candlestick> "candlesticks"

    let save (db: IDbConnection) (candles: Candlestick list) =
        task {
            if candles.IsEmpty then
                return 0
            else
                return!
                    insert {
                        into candlesticks
                        values candles
                    }
                    |> db.InsertAsync<Candlestick>
        }

    let query (db: IDbConnection) symbol (marketType: MarketType) timeframe from to' limit =
        task {
            let! result =
                select {
                    for c in candlesticks do
                        where (c.Symbol = symbol && c.MarketType = int marketType && c.Timeframe = timeframe)
                        orderByDescending c.Timestamp
                        take (defaultArg limit 1000)
                }
                |> db.SelectAsync<Candlestick>

            return result |> Seq.toList
        }

    let getLatest (db: IDbConnection) symbol (marketType: MarketType) timeframe =
        task {
            let! result =
                select {
                    for c in candlesticks do
                        where (c.Symbol = symbol && c.MarketType = int marketType && c.Timeframe = timeframe)
                        orderByDescending c.Timestamp
                        take 1
                }
                |> db.SelectAsync<Candlestick>

            return Seq.tryHead result
        }

    type T =
        {
            Save: Candlestick list -> Task<int>
            Query:
                string
                    -> MarketType
                    -> string
                    -> DateTime option
                    -> DateTime option
                    -> int option
                    -> Task<Candlestick list>
            GetLatest: string -> MarketType -> string -> Task<Candlestick option>
        }

    let create (db: IDbConnection) : T = { Save = save db; Query = query db; GetLatest = getLatest db }

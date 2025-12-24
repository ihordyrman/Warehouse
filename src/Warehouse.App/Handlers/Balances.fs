module Warehouse.App.Functional.Handlers.Balances

open System.Data
open System.Threading
open Dapper
open Falco
open Warehouse.Core.Functional.Markets.Contracts
open Warehouse.Core.Functional.Markets.Domain

let total: HttpHandler =
    fun ctx ->
        try
            let db = ctx.Plug<IDbConnection>()
            let balanceManager = ctx.Plug<IBalanceManager>()

            db.Query<int>("SELECT DISTINCT type FROM markets")
            |> Seq.map enum<MarketType>
            |> Seq.map (fun marketType ->
                task {
                    let! result = balanceManager.GetTotalUsdtValueAsync(marketType, CancellationToken.None)

                    match result with
                    | Some value -> return value
                    | None -> return 0M
                }
                |> Async.AwaitTask
                |> Async.RunSynchronously
            )
            |> Seq.sum
            |> fun totalBalance -> Response.ofPlainText (string totalBalance) ctx

        with ex ->
            let log = ctx.Plug<Serilog.ILogger>()
            log.Error(ex, "Error getting total balance")
            Response.ofPlainText "0" ctx

module Warehouse.App.Functional.Handlers.Balances

open System.Data
open System.Threading
open Dapper.FSharp.PostgreSQL
open Falco
open Serilog
open Warehouse.Core
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Markets.BalanceManager

let marketsTable = table'<Market> "markets"

let total: HttpHandler =
    fun ctx ->
        try
            use db = ctx.Plug<IDbConnection>()
            let balanceManager = CompositionRoot.createBalanceManager ctx.RequestServices

            select {
                for m in marketsTable do
                    selectAll
            }
            |> db.SelectAsync<Market>
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Seq.map (fun market ->
                task {
                    let! result = BalanceManager.getTotalUsdtValue balanceManager market.Type CancellationToken.None

                    match result with
                    | Ok value -> return value
                    | Error err ->
                        let log = ctx.Plug<ILogger>()
                        log.Error("Error getting balance for {MarketType}: {Error}", market.Type, err)
                        return 0M
                }
                |> Async.AwaitTask
                |> Async.RunSynchronously
            )
            |> Seq.sum
            |> fun totalBalance -> Response.ofPlainText (string totalBalance) ctx

        with ex ->
            let log = ctx.Plug<ILogger>()
            log.Error(ex, "Error getting total balance")
            Response.ofPlainText "0" ctx

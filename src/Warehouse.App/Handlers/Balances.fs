module Warehouse.App.Handlers.Balances

open Dapper.FSharp.PostgreSQL
open Falco
open Microsoft.Extensions.DependencyInjection
open Serilog
open Warehouse.Core.Domain
open Warehouse.Core.Queries

let marketsTable = table'<Market> "markets"

let total: HttpHandler =
    fun ctx ->
        try
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()
            use scope = scopeFactory.CreateScope()

            DashboardQueries.create(scopeFactory).TotalBalanceUsdt()
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> fun totalBalance -> Response.ofPlainText (string totalBalance) ctx

        with ex ->
            let log = ctx.Plug<ILogger>()
            log.Error(ex, "Error getting total balance")
            Response.ofPlainText "0" ctx

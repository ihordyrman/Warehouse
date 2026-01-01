module Warehouse.App.Handlers.Balances

open System.Threading
open Dapper.FSharp.PostgreSQL
open Falco
open Microsoft.Extensions.DependencyInjection
open Serilog
open Warehouse.Core.Domain
open Warehouse.Core.Markets.Services
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


let market (marketType: int) : HttpHandler =
    fun ctx ->
        try
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()
            use scope = scopeFactory.CreateScope()
            let balanceManager = scope.ServiceProvider.GetRequiredService<BalanceManager.T>()
            let marketType = enum<MarketType> marketType

            let totalBalance =
                BalanceManager.getTotalUsdtValue balanceManager marketType CancellationToken.None
                |> Async.AwaitTask
                |> Async.RunSynchronously

            match totalBalance with
            | Ok value -> Response.ofPlainText (string value) ctx
            | Result.Error err ->
                let log = scope.ServiceProvider.GetService<ILogger>()
                log.Error("Error getting balance for {MarketType}: {Error}", marketType, err)
                Response.ofPlainText "0" ctx

        with ex ->
            let log = ctx.Plug<ILogger>()
            log.Error(ex, "Error getting market balance")
            Response.ofPlainText "0" ctx

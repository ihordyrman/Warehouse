module Warehouse.App.Handlers.Accounts

open Falco
open Microsoft.Extensions.DependencyInjection
open Warehouse.Core.Queries

let count: HttpHandler =
    fun ctx ->
        try
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()

            let activeAccounts =
                (DashboardQueries.create scopeFactory).CountMarkets()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> string

            Response.ofPlainText activeAccounts ctx
        with ex ->
            let log = ctx.Plug<Serilog.ILogger>()
            log.Error(ex, "Error getting active accounts")
            Response.ofPlainText "0" ctx

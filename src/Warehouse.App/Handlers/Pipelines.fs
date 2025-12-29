module Warehouse.App.Handlers.Pipelines

open Falco
open Microsoft.Extensions.DependencyInjection
open Warehouse.Core.Queries

let count: HttpHandler =
    fun ctx ->
        try
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()

            let pipelines =
                (DashboardQueries.create scopeFactory).CountPipelines()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> string

            Response.ofPlainText pipelines ctx
        with ex ->
            let log = ctx.Plug<Serilog.ILogger>()
            log.Error(ex, "Error getting active pipelines")
            Response.ofPlainText "0" ctx

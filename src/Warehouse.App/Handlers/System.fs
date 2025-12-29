module Warehouse.App.Handlers.System

open Falco
open Microsoft.Extensions.DependencyInjection
open Serilog
open Warehouse.Core.Queries

type private Status =
    | Idle
    | Online
    | Error

let private getStatus (scopeFactory: IServiceScopeFactory) (log: ILogger) : Status =
    try
        use scope = scopeFactory.CreateScope()

        let enabledPipelines =
            (DashboardQueries.create scopeFactory).CountEnabledPipelines()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        match enabledPipelines with
        | x when x > 0 -> Online
        | _ -> Idle
    with ex ->
        log.Error(ex, "Error getting system status")
        Error

let status: HttpHandler =
    fun ctx ->
        let scopeFactory = ctx.Plug<IServiceScopeFactory>()
        let log = ctx.Plug<ILogger>()
        let status = getStatus scopeFactory log

        Response.ofPlainText
            (match status with
             | Idle -> "Idle"
             | Online -> "Online"
             | Error -> "Error")
            ctx

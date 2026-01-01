module Warehouse.App.Handlers.System

open System.Threading.Tasks
open Falco
open Microsoft.Extensions.DependencyInjection
open Serilog
open Warehouse.Core.Queries

type private Status =
    | Idle
    | Online
    | Error

let private getStatus (scopeFactory: IServiceScopeFactory) (log: ILogger) : Task<Status> =
    task {
        try
            match! (DashboardQueries.create scopeFactory).CountEnabledPipelines() with
            | x when x > 0 -> return Online
            | _ -> return Idle
        with ex ->
            log.Error(ex, "Error getting system status")
            return Error
    }

let status: HttpHandler =
    fun ctx ->
        task {
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()
            let log = ctx.Plug<ILogger>()
            let! status = getStatus scopeFactory log

            return
                Response.ofPlainText
                    (match status with
                     | Idle -> "Idle"
                     | Online -> "Online"
                     | Error -> "Error")
                    ctx
        }

module Warehouse.App.Handlers.System

open System.Threading.Tasks
open Falco
open Falco.Htmx
open Falco.Markup
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

let private statusBadge (status: Status) =
    let text, badgeClass, dotClass =
        match status with
        | Online -> "Online", "bg-green-100 text-green-800", "bg-green-400"
        | Idle -> "Idle", "bg-yellow-100 text-yellow-800", "bg-yellow-400"
        | Error -> "Error", "bg-red-100 text-red-800", "bg-red-400"

    _span [
        _id_ "system-status"
        _class_ $"inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium {badgeClass}"
        Hx.get "/system-status"
        Hx.trigger "every 30s"
        Hx.swapOuterHtml
    ] [ _span [ _class_ $"w-2 h-2 rounded-full mr-1.5 {dotClass}" ] []; Text.raw text ]

let status: HttpHandler =
    fun ctx ->
        task {
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()
            let log = ctx.Plug<ILogger>()
            let! status = getStatus scopeFactory log
            return! Response.ofHtml (statusBadge status) ctx
        }

namespace Warehouse.App.Pages.SystemStatus

open Falco
open Microsoft.Extensions.DependencyInjection
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Warehouse.Core.Queries
open Falco.Markup
open Falco.Htmx

type Status =
    | Idle
    | Online
    | Error

module Data =
    let getStatus (scopeFactory: IServiceScopeFactory) (logger: ILogger option) : Task<Status> =
        task {
            try
                let! enabledCount = (DashboardQueries.create scopeFactory).CountEnabledPipelines()

                return
                    match enabledCount with
                    | x when x > 0 -> Online
                    | _ -> Idle
            with ex ->
                logger |> Option.iter _.LogError(ex, "Error getting system status")
                return Error
        }

module View =
    let private statusConfig =
        function
        | Online -> "Online", "bg-green-100 text-green-800", "bg-green-400"
        | Idle -> "Idle", "bg-yellow-100 text-yellow-800", "bg-yellow-400"
        | Error -> "Error", "bg-red-100 text-red-800", "bg-red-400"

    let statusBadge (status: Status) =
        let text, badgeClass, dotClass = statusConfig status

        _span [
            _id_ "system-status"
            _class_ $"inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium {badgeClass}"
            Hx.get "/system-status"
            Hx.trigger "every 30s"
            Hx.swapOuterHtml
        ] [ _span [ _class_ $"w-2 h-2 rounded-full mr-1.5 {dotClass}" ] []; Text.raw text ]

    let statusPlaceholder =
        _span [
            _id_ "system-status"
            _class_ "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800"
            Hx.get "/system-status"
            Hx.trigger "load, every 30s"
            Hx.swapOuterHtml
        ] [
            _span [ _class_ "w-2 h-2 rounded-full mr-1.5 bg-gray-400 animate-pulse" ] []
            Text.raw "Loading..."
        ]

module Handler =
    let status: HttpHandler =
        fun ctx ->
            task {
                let scopeFactory = ctx.Plug<IServiceScopeFactory>()
                let logger = ctx.Plug<ILoggerFactory>().CreateLogger("System")
                let! status = Data.getStatus scopeFactory (Some logger)
                return! Response.ofHtml (View.statusBadge status) ctx
            }

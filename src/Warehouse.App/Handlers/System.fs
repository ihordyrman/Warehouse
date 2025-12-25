module Warehouse.App.Handlers.System

open System.Data
open Dapper
open Falco
open Falco.Markup
open Falco.Htmx
open Serilog

type Status = { Text: string; CssClass: string }

let getStatus (db: IDbConnection) (log: ILogger) : Status =
    try
        let enabledPipelines =
            db.QuerySingle<int>("SELECT COUNT(1) FROM pipeline_configurations WHERE enabled = true")

        match enabledPipelines with
        | x when x > 0 -> { Text = "Online"; CssClass = "bg-green-100 text-green-800" }
        | _ -> { Text = "Idle"; CssClass = "bg-yellow-100 text-yellow-800" }
    with ex ->
        log.Error(ex, "Error getting system status")
        { Text = "Error"; CssClass = "bg-red-100 text-red-800" }

let status: HttpHandler =
    fun ctx ->
        let db = ctx.Plug<IDbConnection>()
        let log = ctx.Plug<ILogger>()
        let status = getStatus db log

        let statusCss =
            match status.Text with
            | "Online" -> "bg-green-400"
            | "Idle" -> "bg-yellow-400"
            | _ -> "bg-red-400"

        let html =
            _span [
                _id_ "system-status"
                _class_ $"inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium {status.CssClass}"
                Hx.get "/system-status"
                Hx.trigger "every 30s"
                Hx.swapOuterHtml
            ] [ _span [ _class_ $"w-2 h-2 rounded-full mr-1.5 {statusCss}" ] []; Text.raw status.Text ]

        Response.ofHtml html ctx

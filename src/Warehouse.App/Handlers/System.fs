module Warehouse.App.Handlers.System

open System.Data
open Dapper
open Falco
open Serilog

type private Status =
    | Idle
    | Online
    | Error

let private getStatus (db: IDbConnection) (log: ILogger) : Status =
    try
        let enabledPipelines =
            db.QuerySingle<int>("SELECT COUNT(1) FROM pipeline_configurations WHERE enabled = true")

        match enabledPipelines with
        | x when x > 0 -> Online
        | _ -> Idle
    with ex ->
        log.Error(ex, "Error getting system status")
        Error

let status: HttpHandler =
    fun ctx ->
        let db = ctx.Plug<IDbConnection>()
        let log = ctx.Plug<ILogger>()
        let status = getStatus db log

        Response.ofPlainText
            (match status with
             | Idle -> "Idle"
             | Online -> "Online"
             | Error -> "Error")
            ctx

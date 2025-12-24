module Warehouse.App.Functional.Handlers.Pipelines

open System.Data
open Dapper
open Falco

let count: HttpHandler =
    fun ctx ->
        try
            let db = ctx.Plug<IDbConnection>()
            let pipelines = db.QuerySingle<int>("SELECT COUNT(1) FROM public.pipeline_configurations")
            Response.ofPlainText (string pipelines) ctx
        with ex ->
            let log = ctx.Plug<Serilog.ILogger>()
            log.Error(ex, "Error getting active pipelines")
            Response.ofPlainText "0" ctx

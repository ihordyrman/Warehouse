module Warehouse.App.Functional.Handlers.Accounts

open System.Data
open Dapper
open Falco

let count: HttpHandler =
    fun ctx ->
        try
            let db = ctx.Plug<IDbConnection>()
            let activeAccounts = db.QuerySingle<int>("SELECT COUNT(1) FROM markets")
            Response.ofPlainText (string activeAccounts) ctx
        with ex ->
            let log = ctx.Plug<Serilog.ILogger>()
            log.Error(ex, "Error getting active accounts")
            Response.ofPlainText "0" ctx

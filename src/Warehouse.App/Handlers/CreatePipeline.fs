module Warehouse.App.Handlers.CreatePipeline

open Falco
open Falco.Markup
open Warehouse.Core.Domain

type CreatePipelineInput = { MarketType: int; Symbol: string; Tags: string; ExecutionInterval: int; Enabled: bool }

let private successResponse (symbol: string) =
    _div [ _class_ "mt-4 p-4 bg-green-50 border border-green-200 rounded-md" ] [
        _div [ _class_ "flex items-center" ] [
            _i [ _class_ "fas fa-check-circle text-green-500 mr-2" ] []
            _span [ _class_ "text-green-800 font-medium" ] [ Text.raw $"Pipeline for {symbol} created successfully!" ]
        ]
        _p [ _class_ "text-green-700 text-sm mt-2" ] [ Text.raw "Redirecting to dashboard..." ]
        _script [] [ Text.raw "setTimeout(() => window.location.href = '/', 2000);" ]
    ]

let private errorResponse (message: string) =
    _div [ _class_ "mt-4 p-4 bg-red-50 border border-red-200 rounded-md" ] [
        _div [ _class_ "flex items-center" ] [
            _i [ _class_ "fas fa-exclamation-circle text-red-500 mr-2" ] []
            _span [ _class_ "text-red-800 font-medium" ] [ Text.raw message ]
        ]
    ]

let create: HttpHandler =
    fun ctx ->
        task {
            let! form = Request.getForm ctx

            let marketType = form.TryGetInt "marketType" |> Option.defaultValue (int MarketType.Okx)

            let symbol = form.TryGetString "symbol" |> Option.defaultValue ""

            let tags = form.TryGetString "tags" |> Option.defaultValue ""

            let executionInterval = form.TryGetInt "executionInterval" |> Option.defaultValue 5

            let enabled = form.TryGetString "enabled" |> Option.map (fun _ -> true) |> Option.defaultValue false

            // todo: Add actual pipeline creation logic here
            // for now, just return success response
            if System.String.IsNullOrWhiteSpace(symbol) then
                return! Response.ofHtml (errorResponse "Symbol is required") ctx
            else
                return! Response.ofHtml (successResponse symbol) ctx
        }

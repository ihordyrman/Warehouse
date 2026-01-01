module Warehouse.App.Handlers.CreatePipeline

open Falco
open Falco.Markup
open Falco.Htmx
open Warehouse.Core.Domain

type CreatePipelineInput = { MarketType: int; Symbol: string; Tags: string; ExecutionInterval: int; Enabled: bool }

let private successResponse (symbol: string) =
    _div [ _id_ "pipeline-modal"; _class_ "fixed inset-0 z-50 overflow-y-auto" ] [
        _div [ _class_ "fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" ] []
        _div [ _class_ "fixed inset-0 z-10 overflow-y-auto" ] [
            _div [ _class_ "flex min-h-full items-center justify-center p-4" ] [
                _div [
                    _class_
                        "relative transform overflow-hidden rounded-xl bg-white shadow-2xl w-full max-w-md p-6 text-center"
                ] [
                    _div [ _class_ "mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-100 mb-4" ] [
                        _i [ _class_ "fas fa-check text-3xl text-green-600" ] []
                    ]
                    _h3 [ _class_ "text-lg font-semibold text-gray-900 mb-2" ] [ Text.raw "Pipeline Created!" ]
                    _p [ _class_ "text-gray-600 mb-4" ] [
                        Text.raw $"Pipeline for {symbol} has been created successfully."
                    ]
                    _button [
                        _type_ "button"
                        _class_
                            "px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors"
                        Hx.get "/pipelines/modal/close"
                        Hx.targetCss "#modal-container"
                        Hx.swapInnerHtml
                        Attr.create "hx-on::after-request" "htmx.trigger('#pipelines-container', 'load')"
                    ] [ Text.raw "Close" ]
                ]
            ]
        ]
    ]

let private errorResponse (message: string) =
    _div [ _id_ "pipeline-modal"; _class_ "fixed inset-0 z-50 overflow-y-auto" ] [
        _div [ _class_ "fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" ] []
        _div [ _class_ "fixed inset-0 z-10 overflow-y-auto" ] [
            _div [ _class_ "flex min-h-full items-center justify-center p-4" ] [
                _div [
                    _class_
                        "relative transform overflow-hidden rounded-xl bg-white shadow-2xl w-full max-w-md p-6 text-center"
                ] [
                    _div [ _class_ "mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-red-100 mb-4" ] [
                        _i [ _class_ "fas fa-exclamation-triangle text-3xl text-red-600" ] []
                    ]
                    _h3 [ _class_ "text-lg font-semibold text-gray-900 mb-2" ] [ Text.raw "Error" ]
                    _p [ _class_ "text-gray-600 mb-4" ] [ Text.raw message ]
                    _div [ _class_ "flex justify-center space-x-3" ] [
                        _button [
                            _type_ "button"
                            _class_
                                "px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 font-medium rounded-lg transition-colors"
                            Hx.get "/pipelines/modal/close"
                            Hx.targetCss "#modal-container"
                            Hx.swapInnerHtml
                        ] [ Text.raw "Close" ]
                        _button [
                            _type_ "button"
                            _class_
                                "px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors"
                            Hx.get "/pipelines/modal"
                            Hx.targetCss "#modal-container"
                            Hx.swapInnerHtml
                        ] [ Text.raw "Try Again" ]
                    ]
                ]
            ]
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

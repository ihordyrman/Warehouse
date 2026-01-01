module Warehouse.App.Views.CreatePipeline

open Falco
open Falco.Markup
open Falco.Htmx
open Warehouse.Core.Domain

let private marketTypes = [ MarketType.Okx; MarketType.Binance ]

let private marketTypeField =
    _div [] [
        _label [ _for_ "marketType"; _class_ "block text-sm font-medium text-gray-700 mb-2" ] [
            Text.raw "Market Type "
            _span [ _class_ "text-red-500" ] [ Text.raw "*" ]
        ]
        _select [
            _id_ "marketType"
            _name_ "marketType"
            _class_
                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        ] [
            for marketType in marketTypes do
                _option [ _value_ (string (int marketType)) ] [ Text.raw (marketType.ToString()) ]
        ]
    ]

let private symbolField =
    _div [] [
        _label [ _for_ "symbol"; _class_ "block text-sm font-medium text-gray-700 mb-2" ] [
            Text.raw "Symbol "
            _span [ _class_ "text-red-500" ] [ Text.raw "*" ]
        ]
        _input [
            _id_ "symbol"
            _name_ "symbol"
            _type_ "text"
            Attr.create "placeholder" "e.g., BTC-USDT, ETH-USDT"
            _class_
                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            Attr.create "required" "required"
        ]
        _p [ _class_ "text-sm text-gray-500 mt-1" ] [
            Text.raw "Enter the trading pair symbol (uppercase letters, numbers, hyphens, and slashes only)"
        ]
    ]

let private tagsField =
    _div [] [
        _label [ _for_ "tags"; _class_ "block text-sm font-medium text-gray-700 mb-2" ] [ Text.raw "Tags" ]
        _input [
            _id_ "tags"
            _name_ "tags"
            _type_ "text"
            Attr.create "placeholder" "e.g., scalping, high-frequency, btc"
            _class_
                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        ]
        _p [ _class_ "text-sm text-gray-500 mt-1" ] [
            Text.raw "Enter tags separated by commas. Tags help organize and filter pipelines."
        ]
    ]

let private executionIntervalField =
    _div [] [
        _label [ _for_ "executionInterval"; _class_ "block text-sm font-medium text-gray-700 mb-2" ] [
            Text.raw "Execution Interval (minutes) "
            _span [ _class_ "text-red-500" ] [ Text.raw "*" ]
        ]
        _input [
            _id_ "executionInterval"
            _name_ "executionInterval"
            _type_ "number"
            Attr.create "min" "1"
            Attr.create "placeholder" "e.g., 5"
            _class_
                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            Attr.create "required" "required"
        ]
        _p [ _class_ "text-sm text-gray-500 mt-1" ] [
            Text.raw "Specify how often the pipeline should execute (in minutes)."
        ]
    ]

let private enabledField =
    _div [ _class_ "flex items-center" ] [
        _input [
            _id_ "enabled"
            _name_ "enabled"
            _type_ "checkbox"
            _class_ "h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
        ]
        _label [ _for_ "enabled"; _class_ "ml-2 block text-sm text-gray-700" ] [
            Text.raw "Enable pipeline immediately"
        ]
    ]

let private helpSection =
    _div [ _class_ "mt-4 bg-blue-50 border border-blue-200 rounded-md p-3" ] [
        _h4 [ _class_ "text-xs font-semibold text-blue-900 mb-1" ] [
            _i [ _class_ "fas fa-info-circle mr-1" ] []
            Text.raw "Tips"
        ]
        _ul [ _class_ "text-xs text-blue-800 space-y-0.5" ] [
            _li [] [ Text.raw "• Symbol must match exchange format (e.g., BTC-USDT)" ]
            _li [] [ Text.raw "• Each market + symbol combination must be unique" ]
        ]
    ]

/// Modal content returned by the handler (loaded via HTMX)
let modalContent =
    _div [
        _id_ "pipeline-modal"
        _class_ "fixed inset-0 z-50 overflow-y-auto"
        Attr.create "aria-labelledby" "modal-title"
        Attr.create "role" "dialog"
        Attr.create "aria-modal" "true"
    ] [
        // backdrop
        _div [
            _class_ "fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
            Hx.get "/pipelines/modal/close"
            Hx.targetCss "#modal-container"
            Hx.swapInnerHtml
        ] []

        // modal panel
        _div [ _class_ "fixed inset-0 z-10 overflow-y-auto" ] [
            _div [ _class_ "flex min-h-full items-center justify-center p-4" ] [
                _div [
                    _class_
                        "relative transform overflow-hidden rounded-xl bg-white shadow-2xl transition-all w-full max-w-lg"
                ] [
                    // header
                    _div [ _class_ "bg-gradient-to-r from-blue-600 to-blue-700 px-6 py-4" ] [
                        _div [ _class_ "flex items-center justify-between" ] [
                            _h3 [ _id_ "modal-title"; _class_ "text-lg font-semibold text-white" ] [
                                _i [ _class_ "fas fa-plus-circle mr-2" ] []
                                Text.raw "Create New Pipeline"
                            ]
                            _button [
                                _type_ "button"
                                _class_ "text-white hover:text-gray-200 transition-colors"
                                Hx.get "/pipelines/modal/close"
                                Hx.targetCss "#modal-container"
                                Hx.swapInnerHtml
                            ] [ _i [ _class_ "fas fa-times text-xl" ] [] ]
                        ]
                        _p [ _class_ "text-blue-100 text-sm mt-1" ] [ Text.raw "Configure a new trading pipeline" ]
                    ]

                    // form
                    _form [
                        _method_ "post"
                        Hx.post "/pipelines/create"
                        Hx.targetCss "#modal-container"
                        Hx.swapInnerHtml
                    ] [
                        _div [ _class_ "px-6 py-4 space-y-4 max-h-[60vh] overflow-y-auto" ] [
                            marketTypeField
                            symbolField
                            tagsField
                            executionIntervalField
                            enabledField
                            helpSection
                        ]

                        // footer with buttons
                        _div [ _class_ "bg-gray-50 px-6 py-4 flex justify-end space-x-3 border-t" ] [
                            _button [
                                _type_ "button"
                                _class_
                                    "px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 font-medium rounded-lg transition-colors"
                                Hx.get "/pipelines/modal/close"
                                Hx.targetCss "#modal-container"
                                Hx.swapInnerHtml
                            ] [ _i [ _class_ "fas fa-times mr-2" ] []; Text.raw "Cancel" ]
                            _button [
                                _type_ "submit"
                                _class_
                                    "px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg shadow-sm hover:shadow-md transition-all"
                            ] [ _i [ _class_ "fas fa-plus mr-2" ] []; Text.raw "Create Pipeline" ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let getModal: HttpHandler = Response.ofHtml modalContent
let getCloseModal: HttpHandler = Response.ofHtml (_div [] [])

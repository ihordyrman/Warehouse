module Warehouse.App.Views.CreatePipeline

open Falco
open Falco.Markup
open Falco.Htmx
open Warehouse.Core.Domain

let private marketTypes = [ MarketType.Okx; MarketType.Binance ]

let private pageHeader =
    _div [ _class_ "mb-6" ] [
        _div [ _class_ "flex items-center mb-2" ] [
            _a [ _href_ "/"; _class_ "text-gray-600 hover:text-gray-900 mr-2" ] [
                _i [ _class_ "fas fa-arrow-left" ] []
            ]
            _h1 [ _class_ "text-2xl font-bold text-gray-900" ] [ Text.raw "Create New Pipeline" ]
        ]
        _p [ _class_ "text-gray-600" ] [
            Text.raw "Configure a new trading pipeline for a specific market and symbol"
        ]
    ]

let private marketTypeField =
    _div [] [
        _label [ _for_ "marketType"; _class_ "block text-sm font-medium text-gray-700 mb-2" ] [
            Text.raw "Market Type "
            _span [ _class_ "text-red-500" ] [ Text.raw "*" ]
        ]
        _select [
            _id_ "marketType"
            _name_ "marketType"
            _class_ "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
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
            _class_ "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            Attr.create "required" "required"
        ]
        _p [ _class_ "text-sm text-gray-500 mt-1" ] [
            Text.raw "Enter the trading pair symbol (uppercase letters, numbers, hyphens, and slashes only)"
        ]
    ]

let private tagsField =
    _div [] [
        _label [ _for_ "tags"; _class_ "block text-sm font-medium text-gray-700 mb-2" ] [
            Text.raw "Tags"
        ]
        _input [
            _id_ "tags"
            _name_ "tags"
            _type_ "text"
            Attr.create "placeholder" "e.g., scalping, high-frequency, btc"
            _class_ "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
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
            _class_ "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
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

let private formButtons =
    _div [ _class_ "flex justify-end space-x-3 pt-4 border-t" ] [
        _a [
            _href_ "/"
            _class_ "inline-flex items-center px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 font-medium rounded-lg shadow-sm transition-all duration-200"
        ] [
            _i [ _class_ "fas fa-times mr-2" ] []
            Text.raw "Cancel"
        ]
        _button [
            _type_ "submit"
            _class_ "inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg shadow-sm hover:shadow-md transition-all duration-200"
        ] [
            _i [ _class_ "fas fa-plus mr-2" ] []
            Text.raw "Create Pipeline"
        ]
    ]

let private pipelineForm =
    _div [ _class_ "card bg-white rounded-xl shadow-sm border border-gray-200 p-6" ] [
        _form [ _method_ "post"; Hx.post "/pipelines/create"; Hx.targetCss "#form-result" ] [
            _div [ _class_ "space-y-6" ] [
                marketTypeField
                symbolField
                tagsField
                executionIntervalField
                enabledField
                formButtons
            ]
        ]
        _div [ _id_ "form-result" ] []
    ]

let private helpSection =
    _div [ _class_ "mt-6 bg-blue-50 border border-blue-200 rounded-md p-4" ] [
        _h3 [ _class_ "text-sm font-semibold text-blue-900 mb-2" ] [
            _i [ _class_ "fas fa-info-circle mr-1" ] []
            Text.raw "Pipeline Configuration Tips"
        ]
        _ul [ _class_ "text-sm text-blue-800 space-y-1" ] [
            _li [] [ Text.raw "• Choose the market type that matches your trading account" ]
            _li [] [ Text.raw "• Symbol must match the exact format used by the exchange (e.g., BTC-USDT for OKX)" ]
            _li [] [ Text.raw "• Pipelines can be enabled/disabled at any time from the Pipelines list" ]
            _li [] [ Text.raw "• Each market type + symbol combination must be unique" ]
        ]
    ]

let content =
    _div [ _class_ "max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-6" ] [
        pageHeader
        pipelineForm
        helpSection
    ]

let get: HttpHandler =
    let html =
        _html [] [
            _head [] [
                _link [ _href_ "./styles.css"; _rel_ "stylesheet" ]
                _link [
                    _href_ "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"
                    _rel_ "stylesheet"
                ]
                _script [ _src_ HtmxScript.cdnSrc ] []
                _script [ _src_ "https://cdn.tailwindcss.com" ] []
            ]
            _body [ _class_ "min-h-screen bg-gray-50" ] [
                Index.header
                content
            ]
        ]

    Response.ofHtml html

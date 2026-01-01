module Warehouse.App.Views.Index

open Falco
open Falco.Markup
open Falco.Htmx

[<Literal>]
let load = "load"

[<Literal>]
let loadEvery30s = "load, every 30s"

let header =
    _header [ _class_ "bg-white shadow-sm border-b border-gray-200" ] [
        _div [ _class_ "max-w-[80%] mx-auto px-4 sm:px-6 lg:px-8" ] [
            _div [ _class_ "flex justify-between items-center h-16" ] [
                _div [ _class_ "flex items-center space-x-8" ] [
                    _h1 [ _class_ "text-xl font-bold text-gray-900" ] [
                        _a [ _href_ "/" ] [ Text.raw "Warehouse System" ]
                    ]
                    _nav [ _class_ "hidden md:flex space-x-4" ] [
                        _a [ _href_ "/"; _class_ "text-gray-700 hover:text-gray-900" ] [ Text.raw "Dashboard" ]
                        _a [ _href_ "/accounts"; _class_ "text-gray-700 hover:text-gray-900" ] [ Text.raw "Accounts" ]
                        _a [ _href_ "/trading"; _class_ "text-gray-700 hover:text-gray-900" ] [ Text.raw "Trading" ]
                        _a [ _href_ "/settings"; _class_ "text-gray-700 hover:text-gray-900" ] [ Text.raw "Settings" ]
                    ]
                ]
                _div [ _class_ "flex items-center space-x-4" ] [
                    _span [ _class_ "text-sm text-gray-500" ] [ Text.raw "Status:" ]
                    SystemView.statusPlaceholder
                    _button [
                        Hx.post "/refresh"
                        Hx.targetCss "#main-content"
                        _class_ "text-gray-500 hover:text-gray-700"
                        Attr.create "aria-label" "Refresh page"
                    ] [ _i [ _class_ "fas fa-sync-alt" ] [] ]
                ]
            ]
        ]
    ]

let activeMarkets =
    _div [
        _class_
            "relative overflow-hidden rounded-xl bg-gradient-to-br from-blue-500 to-blue-600 p-6 shadow-lg
                                 hover:shadow-xl transition-shadow duration-300"
    ] [
        _div [ _class_ "absolute top-0 right-0 -mt-4 -mr-4 h-24 w-24 rounded-full bg-white opacity-10" ] []
        _div [ _class_ "relative" ] [
            _div [ _class_ "flex items-center justify-between mb-2" ] [
                _div [ _class_ "p-3 bg-white bg-opacity-20 rounded-lg backdrop-blur-sm" ] [
                    _i [ _class_ "fas fa-shield-alt text-2xl text-white" ] []
                ]
                _span [ _class_ "text-blue-100 text-sm font-medium" ] [ Text.raw "Markets" ]
            ]
            _div [ _class_ "mt-4" ] [
                _p [
                    _class_ "text-4xl font-bold text-white"
                    Hx.get "/markets/count"
                    Hx.trigger load
                    Hx.swapInnerHtml
                ] [ Text.raw "0" ]
                _p [ _class_ "text-blue-100 text-sm mt-1" ] [ Text.raw "Active Connections" ]
            ]
        ]
    ]

let activePipelines =
    _div [
        _class_
            "relative overflow-hidden rounded-xl bg-gradient-to-br from-green-500 to-emerald-600
                                     p-6 shadow-lg hover:shadow-xl transition-shadow duration-300"
    ] [
        _div [ _class_ "absolute top-0 right-0 -mt-4 -mr-4 h-24 w-24 rounded-full bg-white opacity-10" ] []
        _div [ _class_ "relative" ] [
            _div [ _class_ "flex items-center justify-between mb-2" ] [
                _div [ _class_ "p-3 bg-white bg-opacity-20 rounded-lg backdrop-blur-sm" ] [
                    _i [ _class_ "fas fa-robot text-2xl text-white" ] []
                ]
                _span [ _class_ "text-green-100 text-sm font-medium" ] [ Text.raw "Pipelines" ]
            ]
            _div [ _class_ "mt-4" ] [
                _p [
                    _class_ "text-4xl font-bold text-white"
                    Hx.get "/pipelines/count"
                    Hx.trigger load
                    Hx.swapInnerHtml
                ] [ Text.raw "0" ]
                _p [ _class_ "text-green-100 text-sm mt-1" ] [ Text.raw "Total Pipelines" ]
            ]
        ]
    ]

let totalBalance =
    _div [
        _class_
            "relative overflow-hidden rounded-xl bg-gradient-to-br from-purple-500 to-pink-600
                                     p-6 shadow-lg hover:shadow-xl transition-shadow duration-300"
    ] [
        _div [ _class_ "absolute top-0 right-0 -mt-4 -mr-4 h-24 w-24 rounded-full bg-white opacity-10" ] []
        _div [ _class_ "relative" ] [
            _div [ _class_ "flex items-center justify-between mb-2" ] [
                _div [ _class_ "p-3 bg-white bg-opacity-20 rounded-lg backdrop-blur-sm" ] [
                    _i [ _class_ "fas fa-wallet text-2xl text-white" ] []
                ]
                _span [ _class_ "text-purple-100 text-sm font-medium" ] [ Text.raw "Balance" ]
            ]
            _div [ _class_ "mt-4" ] [
                _p [
                    _class_ "text-4xl font-bold text-white"
                    Hx.get "/balance/total"
                    Hx.trigger load
                    Hx.swapInnerHtml
                ] [ Text.raw "$0.00" ]
                _p [ _class_ "text-purple-100 text-sm mt-1" ] [ Text.raw "Total Portfolio" ]
            ]
        ]
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
                header
                _div [ _id_ "main-content"; _class_ "max-w-[80%] mx-auto px-4 sm:px-6 lg:px-8 py-6" ] [
                    _div [ _class_ "grid grid-cols-1 md:grid-cols-3 gap-6 mb-10" ] [
                        activeMarkets
                        activePipelines
                        totalBalance
                    ]

                    _section [ _class_ "mb-10" ] [
                        _div [ _class_ "flex justify-between items-center mb-6" ] [
                            _div [] [
                                _h2 [ _class_ "text-2xl font-bold text-gray-900" ] [ Text.raw "Market Accounts" ]
                                _p [ _class_ "text-gray-500 text-sm mt-1" ] [
                                    Text.raw "Manage your exchange connections"
                                ]
                            ]
                            _a [
                                // not implemented yet (ex AccountCreate)
                                _href_ "/create-account"
                                _class_
                                    "inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700
                                     text-white font-medium rounded-lg shadow-sm hover:shadow-md
                                     transition-all duration-200"
                            ] [ _i [ _class_ "fas fa-plus mr-2" ] []; Text.raw "Add Account" ]
                        ]

                        _div [ Hx.get "/markets/grid"; Hx.trigger load; Hx.swapInnerHtml; _id_ "accounts-container" ] [
                            _div [ _class_ "flex justify-center py-8" ] [
                                _i [ _class_ "fas fa-spinner fa-spin text-gray-400 text-2xl" ] []
                            ]
                        ]
                    ]

                    _div [ Hx.get "/pipelines/grid"; Hx.trigger load; Hx.swapInnerHtml; _id_ "pipelines-container" ] [
                        _div [ _class_ "flex justify-center py-8" ] [
                            _i [ _class_ "fas fa-spinner fa-spin text-gray-400 text-2xl" ] []
                        ]
                    ]
                ]

                _div [ _id_ "modal-container" ] []
            ]
        ]

    Response.ofHtml html

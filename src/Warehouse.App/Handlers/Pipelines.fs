module Warehouse.App.Handlers.Pipelines

open Falco
open Falco.Htmx
open Falco.Markup
open Microsoft.Extensions.DependencyInjection
open Warehouse.Core.Queries

let count: HttpHandler =
    fun ctx ->
        try
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()

            let pipelines =
                (DashboardQueries.create scopeFactory).CountPipelines()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> string

            Response.ofPlainText pipelines ctx
        with ex ->
            let log = ctx.Plug<Serilog.ILogger>()
            log.Error(ex, "Error getting active pipelines")
            Response.ofPlainText "0" ctx

let pipelinesSection tags marketTypes =
    _section [] [
        // section header
        _div [ _class_ "flex justify-between items-center mb-6" ] [
            _div [] [
                _h1 [ _class_ "text-2xl font-bold text-gray-900" ] [ Text.raw "Trading Pipelines" ]
                _p [ _class_ "text-gray-600" ] [ Text.raw "Manage your automated trading pipelines" ]
            ]
            _button [
                _type_ "button"
                Hx.get "/pipelines/modal"
                Hx.targetCss "#modal-container"
                Hx.swapInnerHtml
                _class_
                    "inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg shadow-sm hover:shadow-md transition-all duration-200"
            ] [ _i [ _class_ "fas fa-plus mr-2" ] []; Text.raw "Add Pipeline" ]
        ]

        // filter bar
        _div [ _class_ "card mb-6" ] [
            _form [
                Hx.get "/pipelines"
                Hx.targetCss "#pipelines-table-body"
                Hx.trigger "load, change, keyup delay:300ms from:input"
            ] [
                _div [ _class_ "flex flex-wrap gap-4" ] [
                    _div [ _class_ "flex-1 min-w-[200px]" ] [
                        _label [ _class_ "block text-sm font-medium text-gray-700 mb-1" ] [ Text.raw "Search Symbol" ]
                        _input [
                            _type_ "text"
                            _name_ "searchTerm"
                            Attr.create "placeholder" "Search by symbol..."
                            _class_
                                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        ]
                    ]
                    _div [ _class_ "min-w-[150px]" ] [
                        _label [ _class_ "block text-sm font-medium text-gray-700 mb-1" ] [ Text.raw "Tag" ]
                        _select [
                            _name_ "filterTag"
                            _class_
                                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        ] [
                            _option [ _value_ "" ] [ Text.raw "All Tags" ]
                            for tag in tags do
                                _option [ _value_ tag ] [ Text.raw tag ]
                        ]
                    ]
                    _div [ _class_ "min-w-[150px]" ] [
                        _label [ _class_ "block text-sm font-medium text-gray-700 mb-1" ] [ Text.raw "Account" ]
                        _select [
                            _name_ "filterAccount"
                            _class_
                                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        ] [
                            _option [ _value_ "" ] [ Text.raw "All Accounts" ]
                            for mt in marketTypes do
                                _option [ _value_ mt ] [ Text.raw mt ]
                        ]
                    ]
                    _div [ _class_ "min-w-[150px]" ] [
                        _label [ _class_ "block text-sm font-medium text-gray-700 mb-1" ] [ Text.raw "Status" ]
                        _select [
                            _name_ "filterStatus"
                            _class_
                                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        ] [
                            _option [ _value_ "" ] [ Text.raw "All Status" ]
                            _option [ _value_ "enabled" ] [ Text.raw "Enabled" ]
                            _option [ _value_ "disabled" ] [ Text.raw "Disabled" ]
                        ]
                    ]
                    _div [ _class_ "min-w-[150px]" ] [
                        _label [ _class_ "block text-sm font-medium text-gray-700 mb-1" ] [ Text.raw "Sort By" ]
                        _select [
                            _name_ "sortBy"
                            _class_
                                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        ] [
                            _option [ _value_ "symbol" ] [ Text.raw "Symbol (A-Z)" ]
                            _option [ _value_ "symbol-desc" ] [ Text.raw "Symbol (Z-A)" ]
                            _option [ _value_ "account" ] [ Text.raw "Account (A-Z)" ]
                            _option [ _value_ "account-desc" ] [ Text.raw "Account (Z-A)" ]
                            _option [ _value_ "status" ] [ Text.raw "Status (Disabled First)" ]
                            _option [ _value_ "status-desc" ] [ Text.raw "Status (Enabled First)" ]
                            _option [ _value_ "updated" ] [ Text.raw "Updated (Oldest)" ]
                            _option [ _value_ "updated-desc" ] [ Text.raw "Updated (Newest)" ]
                        ]
                    ]
                ]
            ]
        ]

        // pipelines table
        _div [ _class_ "card overflow-hidden" ] [
            _div [ _class_ "overflow-x-auto" ] [
                _table [ _class_ "min-w-full divide-y divide-gray-200" ] [
                    _thead [ _class_ "bg-gray-50" ] [
                        _tr [] [
                            _th [
                                _class_ "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            ] [ Text.raw "Symbol" ]
                            _th [
                                _class_ "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            ] [ Text.raw "Account" ]
                            _th [
                                _class_ "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            ] [ Text.raw "Status" ]
                            _th [
                                _class_ "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            ] [ Text.raw "Tags" ]
                            _th [
                                _class_ "px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            ] [ Text.raw "Last Updated" ]
                            _th [
                                _class_
                                    "px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"
                            ] [ Text.raw "Actions" ]
                        ]
                    ]
                    _tbody [ _id_ "pipelines-table-body"; _class_ "bg-white divide-y divide-gray-200" ] [
                        _tr [] [
                            _td [ Attr.create "colspan" "6"; _class_ "px-6 py-8 text-center text-gray-500" ] [
                                _i [ _class_ "fas fa-spinner fa-spin text-2xl mb-2" ] []
                                _p [] [ Text.raw "Loading pipelines..." ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let grid: HttpHandler =
    fun ctx ->
        try
            let scopeFactory = ctx.Plug<IServiceScopeFactory>()

            let tags =
                (DashboardQueries.create scopeFactory).GetAllTags() |> Async.AwaitTask |> Async.RunSynchronously

            let markets =
                (DashboardQueries.create scopeFactory).ActiveMarkets()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> List.map _.Type.ToString()

            let html = pipelinesSection tags markets

            Response.ofHtml html ctx

        with ex ->
            let log = ctx.Plug<Serilog.ILogger>()
            log.Error(ex, "Error getting pipelines grid")
            Response.ofPlainText "Error" ctx

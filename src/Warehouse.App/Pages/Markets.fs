namespace Warehouse.App.Pages.Markets

open System.Threading.Tasks
open Falco
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Falco.Markup
open Falco.Htmx
open Warehouse.Core.Domain
open Warehouse.Core.Queries

type MarketInfo = { Id: int; Type: MarketType; Name: string; Enabled: bool; HasCredentials: bool }

module Data =
    let getCount (scopeFactory: IServiceScopeFactory) : Task<int> =
        (DashboardQueries.create scopeFactory).CountMarkets()

    let getActiveMarkets (scopeFactory: IServiceScopeFactory) : Task<MarketInfo list> =
        task {
            let! markets = (DashboardQueries.create scopeFactory).ActiveMarkets()

            return
                markets
                |> List.map (fun m ->
                    {
                        Id = m.Id
                        Type = m.Type
                        Name = m.Type.ToString()
                        Enabled = true
                        HasCredentials = not (isNull (box m.Credentials))
                    }
                )
        }


module View =
    let emptyState =
        _div
            [ _class_ "bg-white rounded-xl border-2 border-dashed border-gray-300 p-12 text-center" ]
            [
                _div
                    [ _class_ "inline-flex items-center justify-center w-16 h-16 bg-gray-100 rounded-full mb-4" ]
                    [ _i [ _class_ "fas fa-exchange-alt text-3xl text-gray-400" ] [] ]
                _h3 [ _class_ "text-lg font-semibold text-gray-900 mb-2" ] [ Text.raw "No Accounts Yet" ]
                _p [ _class_ "text-gray-500 mb-4" ] [ Text.raw "Connect your first exchange account to start trading" ]
                _a
                    [
                        _href_ "/create-account"
                        _class_
                            "inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors"
                    ]
                    [ _i [ _class_ "fas fa-plus mr-2" ] []; Text.raw "Add Your First Account" ]
            ]

    let private marketCard (market: MarketInfo) =
        let typeClass =
            match market.Type.ToString() with
            | "Okx" -> "bg-blue-100 text-blue-800"
            | _ -> "bg-gray-100 text-gray-800"

        let statusClass = if market.Enabled then "bg-green-100 text-green-800" else "bg-gray-100 text-gray-600"

        _div
            [
                _class_
                    "bg-white rounded-xl shadow-sm hover:shadow-md transition-shadow duration-200 border border-gray-200 overflow-hidden"
                _id_ $"account-{market.Id}"
            ]
            [
                // Header
                _div
                    [ _class_ "p-4 border-b border-gray-100 bg-gradient-to-r from-gray-50 to-white" ]
                    [
                        _div
                            [ _class_ "flex justify-between items-start mb-3" ]
                            [
                                _div
                                    [ _class_ "flex-1" ]
                                    [
                                        _h3 [ _class_ "text-lg font-bold text-gray-900 mb-1" ] [ Text.raw market.Name ]
                                        _span
                                            [
                                                _class_
                                                    $"inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium {typeClass}"
                                            ]
                                            [
                                                _i [ _class_ "fas fa-exchange-alt mr-1" ] []
                                                Text.raw (market.Type.ToString())
                                            ]
                                    ]
                                _span
                                    [
                                        _class_
                                            $"inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold {statusClass}"
                                    ]
                                    [
                                        if market.Enabled then
                                            _i [ _class_ "fas fa-check-circle mr-1" ] []
                                            Text.raw "Active"
                                        else
                                            _i [ _class_ "fas fa-pause-circle mr-1" ] []
                                            Text.raw "Inactive"
                                    ]
                            ]
                    ]

                // Body
                _div
                    [ _class_ "p-4 space-y-3" ]
                    [
                        // Credentials status
                        _div
                            [ _class_ "flex items-center" ]
                            [
                                if market.HasCredentials then
                                    _div
                                        [ _class_ "flex items-center text-sm text-green-600" ]
                                        [
                                            _i [ _class_ "fas fa-check-circle mr-2" ] []
                                            _span [ _class_ "font-medium" ] [ Text.raw "API Credentials Configured" ]
                                        ]
                                else
                                    _div
                                        [ _class_ "flex items-center text-sm text-amber-600" ]
                                        [
                                            _i [ _class_ "fas fa-exclamation-triangle mr-2" ] []
                                            _span [ _class_ "font-medium" ] [ Text.raw "No API Credentials" ]
                                        ]
                            ]

                        // Balance section
                        _div
                            [ _class_ "pt-4 border-t border-gray-100" ]
                            [
                                _div
                                    [ _class_ "flex items-center justify-between mb-3" ]
                                    [
                                        _span [ _class_ "text-sm font-semibold text-gray-700" ] [ Text.raw "Balance" ]
                                        _i [ _class_ "fas fa-sync-alt text-gray-400 text-xs" ] []
                                    ]
                                _div
                                    [
                                        Hx.get $"/balance/{int market.Type}"
                                        Hx.trigger "load, every 60s"
                                        Hx.swapInnerHtml
                                        _class_ "space-y-2"
                                    ]
                                    [
                                        _div
                                            [ _class_ "flex justify-center py-2" ]
                                            [ _i [ _class_ "fas fa-spinner fa-spin text-gray-400" ] [] ]
                                    ]
                            ]
                    ]

                // Footer
                _div
                    [ _class_ "px-4 py-3 bg-gray-50 border-t border-gray-100 flex justify-end space-x-3" ]
                    [
                        _a
                            [
                                _href_ $"/accounts/{market.Id}"
                                _class_
                                    "inline-flex items-center text-sm font-medium text-blue-600 hover:text-blue-700 transition-colors"
                            ]
                            [ _i [ _class_ "fas fa-info-circle mr-1.5" ] []; Text.raw "Details" ]
                        _a
                            [
                                _href_ $"/accounts/{market.Id}/edit"
                                _class_
                                    "inline-flex items-center text-sm font-medium text-gray-600 hover:text-gray-700 transition-colors"
                            ]
                            [ _i [ _class_ "fas fa-edit mr-1.5" ] []; Text.raw "Edit" ]
                    ]
            ]

    let grid (markets: MarketInfo list) =
        match markets with
        | [] -> emptyState
        | items ->
            _div
                [ _class_ "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"; _id_ "accounts-grid" ]
                [
                    for market in items do
                        marketCard market
                ]

    let count (n: int) = Text.raw (string n)

module Handler =
    let count: HttpHandler =
        fun ctx ->
            task {
                try
                    let scopeFactory = ctx.Plug<IServiceScopeFactory>()
                    let! count = Data.getCount scopeFactory
                    return! Response.ofHtml (View.count count) ctx
                with ex ->
                    let logger = ctx.Plug<ILoggerFactory>().CreateLogger("Markets")
                    logger.LogError(ex, "Error getting markets count")
                    return! Response.ofHtml (View.count 0) ctx
            }

    let grid: HttpHandler =
        fun ctx ->
            task {
                try
                    let scopeFactory = ctx.Plug<IServiceScopeFactory>()
                    let! markets = Data.getActiveMarkets scopeFactory
                    return! Response.ofHtml (View.grid markets) ctx
                with ex ->
                    let logger = ctx.Plug<ILoggerFactory>().CreateLogger("Markets")
                    logger.LogError(ex, "Error getting markets grid")
                    return! Response.ofHtml View.emptyState ctx
            }

namespace Warehouse.App.Pages.Pipeline

open System
open System.Threading.Tasks
open Falco
open Falco.Htmx
open Falco.Markup
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Queries

type PipelineListItem =
    {
        Id: int
        Symbol: string
        MarketType: MarketType
        Enabled: bool
        Tags: string list
        UpdatedAt: DateTime
    }

type PipelinesGridData =
    {
        Tags: string list
        MarketTypes: string list
        Pipelines: PipelineListItem list
    }

    static member Empty = { Tags = []; MarketTypes = []; Pipelines = [] }

type PipelineFilters =
    {
        SearchTerm: string option
        Tag: string option
        MarketType: string option
        Status: string option
        SortBy: string
    }

    static member Empty = { SearchTerm = None; Tag = None; MarketType = None; Status = None; SortBy = "symbol" }

module Data =
    let getTags (scopeFactory: IServiceScopeFactory) : Task<string list> =
        (DashboardQueries.create scopeFactory).GetAllTags()

    let getMarketTypes (scopeFactory: IServiceScopeFactory) : Task<string list> =
        task {
            let! markets = (DashboardQueries.create scopeFactory).ActiveMarkets()
            return markets |> List.map (fun m -> m.Type.ToString())
        }

    let getCount (scopeFactory: IServiceScopeFactory) : Task<int> =
        (DashboardQueries.create scopeFactory).CountPipelines()

    let getGridData (scopeFactory: IServiceScopeFactory) : Task<PipelinesGridData> =
        task {
            let! tags = getTags scopeFactory
            let! marketTypes = getMarketTypes scopeFactory
            // TODO: add actual pipeline fetching when query is implemented
            return { Tags = tags; MarketTypes = marketTypes; Pipelines = [] }
        }

    let getFilteredPipelines
        (scopeFactory: IServiceScopeFactory)
        (filters: PipelineFilters)
        : Task<PipelineListItem list>
        =
        task {
            let status =
                match filters.Status with
                | Some "enabled" -> Some PipelineStatus.Running
                | Some "disabled" -> Some PipelineStatus.Paused
                | _ -> None

            let! pipelines =
                (DashboardQueries.create scopeFactory).GetPipelines
                    filters.SearchTerm
                    filters.Tag
                    filters.MarketType
                    status
                    filters.SortBy

            return
                pipelines
                |> List.map (fun p ->
                    {
                        Id = p.Id
                        Symbol = p.Symbol
                        MarketType = p.MarketType
                        Enabled = p.Enabled
                        Tags = p.Tags
                        UpdatedAt = p.UpdatedAt
                    }
                )
        }

module View =
    let private filterSelect name label options =
        _div [ _class_ "min-w-[150px]" ] [
            _label [ _class_ "block text-sm font-medium text-gray-700 mb-1" ] [ Text.raw label ]
            _select [
                _name_ name
                _class_
                    "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            ] [
                yield _option [ _value_ "" ] [ Text.raw $"All {label}" ]
                for opt in options do
                    yield _option [ _value_ opt ] [ Text.raw opt ]
            ]
        ]

    let private sectionHeader =
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

    let private filterBar (data: PipelinesGridData) =
        _div [ _class_ "card mb-6" ] [
            _form [
                Hx.get "/pipelines/table"
                Hx.targetCss "#pipelines-table-body"
                Hx.trigger "change, keyup delay:300ms from:input"
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
                    filterSelect "filterTag" "Tag" data.Tags
                    filterSelect "filterAccount" "Account" data.MarketTypes
                    filterSelect "filterStatus" "Status" [ "enabled"; "disabled" ]
                    filterSelect "sortBy" "Sort By" [
                        "symbol"
                        "symbol-desc"
                        "account"
                        "account-desc"
                        "status"
                        "status-desc"
                        "updated"
                        "updated-desc"
                    ]
                ]
            ]
        ]

    let private tableHeader =
        _thead [ _class_ "bg-gray-50" ] [
            _tr [] [
                for (text, align) in
                    [
                        "Symbol", "left"
                        "Account", "left"
                        "Status", "left"
                        "Tags", "left"
                        "Last Updated", "left"
                        "Actions", "right"
                    ] do
                    _th [ _class_ $"px-6 py-3 text-{align} text-xs font-medium text-gray-500 uppercase tracking-wider" ] [
                        Text.raw text
                    ]
            ]
        ]

    let pipelineRow (pipeline: PipelineListItem) =
        let statusClass, statusText =
            if pipeline.Enabled then
                "bg-green-100 text-green-800", "Enabled"
            else
                "bg-gray-100 text-gray-600", "Disabled"

        _tr [ _id_ $"pipeline-{pipeline.Id}"; _class_ "hover:bg-gray-50" ] [
            _td [ _class_ "px-6 py-4 whitespace-nowrap" ] [
                _span [ _class_ "font-medium text-gray-900" ] [ Text.raw pipeline.Symbol ]
            ]
            _td [ _class_ "px-6 py-4 whitespace-nowrap text-sm text-gray-500" ] [
                Text.raw (pipeline.MarketType.ToString())
            ]
            _td [ _class_ "px-6 py-4 whitespace-nowrap" ] [
                _span [ _class_ $"px-2 py-1 text-xs font-medium rounded-full {statusClass}" ] [ Text.raw statusText ]
            ]
            _td [ _class_ "px-6 py-4 whitespace-nowrap" ] [
                _div [ _class_ "flex gap-1" ] [
                    for tag in pipeline.Tags |> List.truncate 3 do
                        _span [ _class_ "px-2 py-0.5 text-xs bg-blue-100 text-blue-800 rounded" ] [ Text.raw tag ]
                ]
            ]
            _td [ _class_ "px-6 py-4 whitespace-nowrap text-sm text-gray-500" ] [
                Text.raw (pipeline.UpdatedAt.ToString("MMM dd, HH:mm"))
            ]
            _td [ _class_ "px-6 py-4 whitespace-nowrap text-right text-sm" ] [
                _a [ _href_ $"/pipelines/{pipeline.Id}"; _class_ "text-blue-600 hover:text-blue-800 mr-3" ] [
                    Text.raw "Edit"
                ]
                _button [
                    _class_ "text-red-600 hover:text-red-800"
                    Hx.delete $"/pipelines/{pipeline.Id}"
                    Hx.confirm "Are you sure you want to delete this pipeline?"
                    Hx.targetCss $"#pipeline-{pipeline.Id}"
                    Hx.swapOuterHtml
                ] [ Text.raw "Delete" ]
            ]
        ]

    let emptyState =
        _tr [] [
            _td [ Attr.create "colspan" "6"; _class_ "px-6 py-12 text-center" ] [
                _div [ _class_ "text-gray-400" ] [
                    _i [ _class_ "fas fa-robot text-4xl mb-3" ] []
                    _p [ _class_ "text-lg font-medium" ] [ Text.raw "No pipelines yet" ]
                    _p [ _class_ "text-sm" ] [ Text.raw "Create your first trading pipeline to get started" ]
                ]
            ]
        ]

    let loadingState =
        _tr [] [
            _td [ Attr.create "colspan" "6"; _class_ "px-6 py-8 text-center text-gray-500" ] [
                _i [ _class_ "fas fa-spinner fa-spin text-2xl mb-2" ] []
                _p [] [ Text.raw "Loading pipelines..." ]
            ]
        ]

    let tableBody (pipelines: PipelineListItem list) =
        match pipelines with
        | _ -> emptyState
    // | items -> Elem.fragment [
    //                 for pipeline in items do
    //                     pipelineRow pipeline
    //             ]

    let private pipelinesTable =
        _div [ _class_ "card overflow-hidden" ] [
            _div [ _class_ "overflow-x-auto" ] [
                _table [ _class_ "min-w-full divide-y divide-gray-200" ] [
                    tableHeader
                    _tbody [
                        _id_ "pipelines-table-body"
                        _class_ "bg-white divide-y divide-gray-200"
                        Hx.get "/pipelines/table"
                        Hx.trigger "load"
                        Hx.swapInnerHtml
                    ] [ loadingState ]
                ]
            ]
        ]

    let section (data: PipelinesGridData) = _section [] [ sectionHeader; filterBar data; pipelinesTable ]

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
                    let logger = ctx.Plug<ILoggerFactory>().CreateLogger("Pipelines")
                    logger.LogError(ex, "Error getting pipelines count")
                    return! Response.ofHtml (View.count 0) ctx
            }

    let grid: HttpHandler =
        fun ctx ->
            task {
                try
                    let scopeFactory = ctx.Plug<IServiceScopeFactory>()
                    let! data = Data.getGridData scopeFactory
                    return! Response.ofHtml (View.section data) ctx
                with ex ->
                    let logger = ctx.Plug<ILoggerFactory>().CreateLogger("Pipelines")
                    logger.LogError(ex, "Error getting pipelines grid")
                    return! Response.ofHtml (View.section PipelinesGridData.Empty) ctx
            }

    let tryGetQueryStringValue (query: IQueryCollection) (key: string) : string option =
        match query.TryGetValue key with
        | true, values when values.Count > 0 -> Some values.[0]
        | _ -> None

    let table: HttpHandler =
        fun ctx ->
            task {
                try
                    let scopeFactory = ctx.Plug<IServiceScopeFactory>()

                    let filters: PipelineFilters =
                        {
                            SearchTerm = tryGetQueryStringValue ctx.Request.Query "searchTerm"
                            Tag = tryGetQueryStringValue ctx.Request.Query "filterTag"
                            MarketType = tryGetQueryStringValue ctx.Request.Query "filterAccount"
                            Status = tryGetQueryStringValue ctx.Request.Query "filterStatus"
                            SortBy = tryGetQueryStringValue ctx.Request.Query "sortBy" |> Option.defaultValue "symbol"
                        }

                    let! pipelines = Data.getFilteredPipelines scopeFactory filters
                    return! Response.ofHtml (View.tableBody pipelines) ctx
                with ex ->
                    let logger = ctx.Plug<ILoggerFactory>().CreateLogger("Pipelines")
                    logger.LogError(ex, "Error getting filtered pipelines")
                    return! Response.ofHtml View.emptyState ctx
            }

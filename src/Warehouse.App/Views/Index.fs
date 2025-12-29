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

let activeAccounts =
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
                _span [ _class_ "text-blue-100 text-sm font-medium" ] [ Text.raw "Accounts" ]
            ]
            _div [ _class_ "mt-4" ] [
                _p [
                    _class_ "text-4xl font-bold text-white"
                    Hx.get "/accounts/count"
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
                        activeAccounts
                        activePipelines
                        totalBalance
                    ]
                ]
            ]
        ]

    Response.ofHtml html

// <section class="mb-10">
//     <div class="flex justify-between items-center mb-6">
//         <div>
//             <h2 class="text-2xl font-bold text-gray-900">Market Accounts</h2>
//             <p class="text-gray-500 text-sm mt-1">Manage your exchange connections</p>
//         </div>
//         <a href="/AccountsCreate"
//            class="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg shadow-sm hover:shadow-md transition-all duration-200">
//             <i class="fas fa-plus mr-2"></i>Add Account
//         </a>
//     </div>
//
//     @if (!Model.Markets.Any())
//     {
//         <div class="bg-white rounded-xl border-2 border-dashed border-gray-300 p-12 text-center">
//             <div class="inline-flex items-center justify-center w-16 h-16 bg-gray-100 rounded-full mb-4">
//                 <i class="fas fa-exchange-alt text-3xl text-gray-400"></i>
//             </div>
//             <h3 class="text-lg font-semibold text-gray-900 mb-2">No Accounts Yet</h3>
//             <p class="text-gray-500 mb-4">Connect your first exchange account to start trading</p>
//             <a href="/AccountsCreate"
//                class="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors">
//                 <i class="fas fa-plus mr-2"></i>Add Your First Account
//             </a>
//         </div>
//     }
//     else
//     {
//         <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6" id="accounts-grid">
//             @foreach (DashboardModel.MarketInfo market in Model.Markets)
//             {
//                 <div
//                     class="bg-white rounded-xl shadow-sm hover:shadow-md transition-shadow duration-200 border border-gray-200 overflow-hidden"
//                     id="account-@market.Id">
//                     <div class="p-4 border-b border-gray-100 bg-gradient-to-r from-gray-50 to-white">
//                         <div class="flex justify-between items-start mb-3">
//                             <div class="flex-1">
//                                 <h3 class="text-lg font-bold text-gray-900 mb-1">@market.Name</h3>
//                                 <span
//                                     class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium @(market.Type == "OKX" ? "bg-blue-100 text-blue-800" : "bg-gray-100 text-gray-800")">
//                                     <i class="fas fa-exchange-alt mr-1"></i>@market.Type
//                                 </span>
//                             </div>
//                             <span
//                                 class="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold @(market.Enabled ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-600")">
//                                 @if (market.Enabled)
//                                 {
//                                     <i class="fas fa-check-circle mr-1"></i>
//                                     <text>Active</text>
//                                 }
//                                 else
//                                 {
//                                     <i class="fas fa-pause-circle mr-1"></i>
//                                     <text>Inactive</text>
//                                 }
//                             </span>
//                         </div>
//                     </div>
//
//                     <div class="p-4 space-y-3">
//                         <div class="flex items-center">
//                             @if (market.HasCredentials)
//                             {
//                                 <div class="flex items-center text-sm text-green-600">
//                                     <i class="fas fa-check-circle mr-2"></i>
//                                     <span class="font-medium">API Credentials Configured</span>
//                                 </div>
//                             }
//                             else
//                             {
//                                 <div class="flex items-center text-sm text-amber-600">
//                                     <i class="fas fa-exclamation-triangle mr-2"></i>
//                                     <span class="font-medium">No API Credentials</span>
//                                 </div>
//                             }
//                         </div>
//
//                         <div class="pt-4 border-t border-gray-100">
//                             <div class="flex items-center justify-between mb-3">
//                                 <span class="text-sm font-semibold text-gray-700">Balance</span>
//                                 <i class="fas fa-sync-alt text-gray-400 text-xs"></i>
//                             </div>
//                             <div hx-get="/Balance/@market.Type"
//                                  hx-trigger="load, every 60s"
//                                  hx-swap="innerHTML"
//                                  class="space-y-2">
//                                 <div class="flex justify-center py-2">
//                                     <i class="fas fa-spinner fa-spin text-gray-400"></i>
//                                 </div>
//                             </div>
//                         </div>
//                     </div>
//
//                     <div class="px-4 py-3 bg-gray-50 border-t border-gray-100 flex justify-end space-x-3">
//                         <a href="/AccountsDetails?id=@market.Id"
//                            class="inline-flex items-center text-sm font-medium text-blue-600 hover:text-blue-700 transition-colors">
//                             <i class="fas fa-info-circle mr-1.5"></i>Details
//                         </a>
//                         <a href="/AccountsEdit?id=@market.Id"
//                            class="inline-flex items-center text-sm font-medium text-gray-600 hover:text-gray-700 transition-colors">
//                             <i class="fas fa-edit mr-1.5"></i>Edit
//                         </a>
//                     </div>
//                 </div>
//             }
//         </div>
//     }
// </section>
//
// <section>
//     <!-- Page Header -->
//     <div class="flex justify-between items-center mb-6">
//         <div>
//             <h1 class="text-2xl font-bold text-gray-900">Trading Pipelines</h1>
//             <p class="text-gray-600">Manage your automated trading pipelines</p>
//         </div>
//         <a href="/PipelinesCreate"
//            class="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg shadow-sm hover:shadow-md transition-all duration-200">
//             <i class="fas fa-plus mr-2"></i>Add Pipeline
//         </a>
//     </div>
//
//     <!-- Filter Bar -->
//     <div class="card mb-6">
//         <form hx-get="/Pipelines"
//               hx-target="#pipelines-table-body"
//               hx-trigger="load, change, keyup delay:300ms from:input">
//             <div class="flex flex-wrap gap-4">
//                 <div class="flex-1 min-w-[200px]">
//                     <label class="block text-sm font-medium text-gray-700 mb-1">Search Symbol</label>
//                     <input type="text"
//                            name="searchTerm"
//                            placeholder="Search by symbol..."
//                            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
//                 </div>
//                 <div class="min-w-[150px]">
//                     <label class="block text-sm font-medium text-gray-700 mb-1">Tag</label>
//                     <select name="filterTag"
//                             class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
//                         <option value="">All Tags</option>
//                         @foreach (string tag in Model.Tags)
//                         {
//                             <option value="@tag">@tag</option>
//                         }
//                     </select>
//                 </div>
//                 <div class="min-w-[150px]">
//                     <label class="block text-sm font-medium text-gray-700 mb-1">Account</label>
//                     <select name="filterAccount"
//                             class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
//                         <option value="">All Accounts</option>
//                         @foreach (object? account in Enum.GetValuesAsUnderlyingType<MarketType>())
//                         {
//                             string accountType = Enum.GetName(typeof(MarketType), (int)account) ?? "Unknown";
//                             <option value="@accountType">@accountType</option>
//                         }
//                     </select>
//                 </div>
//                 <div class="min-w-[150px]">
//                     <label class="block text-sm font-medium text-gray-700 mb-1">Status</label>
//                     <select name="filterStatus"
//                             class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
//                         <option value="">All Status</option>
//                         <option value="enabled">Enabled</option>
//                         <option value="disabled">Disabled</option>
//                     </select>
//                 </div>
//                 <div class="min-w-[150px]">
//                     <label class="block text-sm font-medium text-gray-700 mb-1">Sort By</label>
//                     <select name="sortBy"
//                             class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
//                         <option value="symbol">Symbol (A-Z)</option>
//                         <option value="symbol-desc">Symbol (Z-A)</option>
//                         <option value="account">Account (A-Z)</option>
//                         <option value="account-desc">Account (Z-A)</option>
//                         <option value="status">Status (Disabled First)</option>
//                         <option value="status-desc">Status (Enabled First)</option>
//                         <option value="updated">Updated (Oldest)</option>
//                         <option value="updated-desc">Updated (Newest)</option>
//                     </select>
//                 </div>
//             </div>
//         </form>
//     </div>
//
//     <!-- Pipelines Table -->
//     <div class="card overflow-hidden">
//         <div class="overflow-x-auto">
//             <table class="min-w-full divide-y divide-gray-200">
//                 <thead class="bg-gray-50">
//                 <tr>
//                     <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
//                         Symbol
//                     </th>
//                     <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
//                         Account
//                     </th>
//                     <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
//                         Status
//                     </th>
//                     <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
//                         Tags
//                     </th>
//                     <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
//                         Last Updated
//                     </th>
//                     <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
//                         Actions
//                     </th>
//                 </tr>
//                 </thead>
//                 <tbody id="pipelines-table-body" class="bg-white divide-y divide-gray-200">
//                 <tr>
//                     <td colspan="6" class="px-6 py-8 text-center text-gray-500">
//                         <i class="fas fa-spinner fa-spin text-2xl mb-2"></i>
//                         <p>Loading pipelines...</p>
//                     </td>
//                 </tr>
//                 </tbody>
//             </table>
//         </div>
//     </div>
// </section>

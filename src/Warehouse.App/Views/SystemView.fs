module Warehouse.App.Views.SystemView

open Falco.Markup
open Falco.Htmx

type private Status =
    | Idle
    | Online
    | Error

let private statusConfig =
    function
    | Online -> "Online", "bg-green-100 text-green-800", "bg-green-400"
    | Idle -> "Idle", "bg-yellow-100 text-yellow-800", "bg-yellow-400"
    | Error -> "Error", "bg-red-100 text-red-800", "bg-red-400"

let private getStatus (status: Status) =
    let text, badgeClass, dotClass = statusConfig status

    _span [
        _id_ "system-status"
        _class_ $"inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium {badgeClass}"
        Hx.get "/system-status"
        Hx.trigger "every 30s"
        Hx.swapOuterHtml
    ] [ _span [ _class_ $"w-2 h-2 rounded-full mr-1.5 {dotClass}" ] []; Text.raw text ]

let statusPlaceholder =
    _span [
        _id_ "system-status"
        _class_ "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800"
        Hx.get "/system-status"
        Hx.trigger "load, every 30s"
        Hx.swapInnerHtml
    ] [
        _span [ _class_ "w-2 h-2 rounded-full mr-1.5 bg-gray-400 animate-pulse" ] []
        Text.raw "Loading..."
    ]

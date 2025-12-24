namespace Warehouse.Core.Functional.Shared

open System.Net
open Warehouse.Core.Functional.Markets.Domain

module Errors =
    type ServiceError =
        | ApiError of message: string * statusCode: HttpStatusCode option
        | NotFound of entity: string
        | NoProvider of MarketType
        | Unexpected of exn

    let message =
        function
        | ApiError(msg, _) -> msg
        | NotFound entity -> $"{entity} not found"
        | NoProvider market -> $"No provider registered for {market}"
        | Unexpected ex -> ex.Message

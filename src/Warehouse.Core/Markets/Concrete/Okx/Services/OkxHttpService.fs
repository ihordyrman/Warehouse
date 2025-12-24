namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System
open System.Collections.Generic
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open System.Web
open Microsoft.Extensions.Logging
open Warehouse.Core.Functional.Markets.Contracts
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Functional.Markets.Okx
open Warehouse.Core.Functional.Shared.Errors

type OkxHttpService
    (logger: ILogger<OkxHttpService>, httpClientFactory: IHttpClientFactory, credentialsProvider: ICredentialsProvider)
    =
    let mutable credentialsOption: MarketCredentials option = None
    let serializerOptions = JsonSerializerOptions()

    let buildRequestPath (parameters: Dictionary<string, string> option) (endpoint: string) =
        match parameters with
        | None -> endpoint
        | Some p when p.Count = 0 -> endpoint
        | Some p ->
            let query = HttpUtility.ParseQueryString(String.Empty)

            for kvp in p do
                query.[kvp.Key] <- kvp.Value

            $"{endpoint}?{query}"

    let createJsonContent (json: string) = new StringContent(json, Encoding.UTF8, "application/json")

    member private this.SendRequestAsync<'T>
        (method: string, endpoint: string, ?parameters: Dictionary<string, string>, ?body: obj)
        : Task<Result<'T, ServiceError>> =
        task {
            try
                match credentialsOption with
                | None ->
                    let! creds = credentialsProvider.GetCredentialsAsync(MarketType.Okx, CancellationToken.None)
                    credentialsOption <- creds
                | Some _ -> ()

                match credentialsOption with
                | None -> return Error(ServiceError.NotFound("Credentials not found"))
                | Some credentials ->

                    use httpClient = httpClientFactory.CreateClient("Okx")
                    let requestPath = buildRequestPath parameters endpoint

                    let bodyJson =
                        match body with
                        | None -> ""
                        | Some b ->
                            JsonSerializer.Serialize(Option.defaultValue (box "") (Option.ofObj b), serializerOptions)

                    let timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")

                    let signature =
                        OkxAuth.generateSignature timestamp credentials.SecretKey method requestPath bodyJson

                    httpClient.DefaultRequestHeaders.Add("OK-ACCESS-SIGN", signature)
                    httpClient.DefaultRequestHeaders.Add("OK-ACCESS-TIMESTAMP", timestamp)
                    httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", credentials.ApiKey)
                    httpClient.DefaultRequestHeaders.Add("OK-ACCESS-PASSPHRASE", credentials.Passphrase)

                    httpClient.DefaultRequestHeaders.Add(
                        "x-simulated-trading",
                        if credentials.IsSandbox then "1" else "0"
                    )

                    let! httpResponse =
                        match method.ToUpperInvariant() with
                        | "GET" -> httpClient.GetAsync(requestPath)
                        | "POST" -> httpClient.PostAsync(requestPath, createJsonContent bodyJson)
                        | _ -> failwith $"Unsupported HTTP method: {method}"

                    if not httpResponse.IsSuccessStatusCode then
                        let! errorContent = httpResponse.Content.ReadAsStringAsync()

                        logger.LogError(
                            "HTTP request failed: {StatusCode} - {Content}",
                            httpResponse.StatusCode,
                            errorContent
                        )

                        return
                            Error(
                                ServiceError.ApiError(
                                    $"HTTP request failed: {errorContent}",
                                    Some httpResponse.StatusCode
                                )
                            )
                    else
                        let! jsonString = httpResponse.Content.ReadAsStringAsync()
                        let response = JsonSerializer.Deserialize<OkxHttpResponse<'T>>(jsonString, serializerOptions)

                        match response.Data with
                        | Some data -> return Ok(data)
                        | None -> return Error(ServiceError.ApiError($"No data in response: {response.Message}", None))
            with ex ->
                logger.LogError(ex, "Error making authenticated request to {Endpoint}", endpoint)
                return Error(ServiceError.ApiError($"Error making authenticated request to {endpoint}", None))
        }

    member this.GetBalanceAsync(?currency: string) =
        let parameters = Dictionary<string, string>()
        currency |> Option.iter (fun c -> parameters.["ccy"] <- c)
        this.SendRequestAsync<OkxBalanceDetail[]>("GET", "/api/v5/asset/balances", parameters)

    member this.GetFundingBalanceAsync(?currency: string) =
        let parameters = Dictionary<string, string>()
        currency |> Option.iter (fun c -> parameters.["ccy"] <- c)
        this.SendRequestAsync<OkxFundingBalance[]>("GET", "/api/v5/asset/balances", parameters)

    member this.GetAccountBalanceAsync(?currency: string) =
        let parameters = Dictionary<string, string>()
        currency |> Option.iter (fun c -> parameters.["ccy"] <- c)
        this.SendRequestAsync<OkxAccountBalance[]>("GET", "/api/v5/account/balance", parameters)

    member this.GetAssetsValuationAsync(?valuationCurrency: string) =
        let currency = defaultArg valuationCurrency "USDT"
        let parameters = Dictionary<string, string>()
        parameters.["ccy"] <- currency
        this.SendRequestAsync<OkxAssetsValuation[]>("GET", "/api/v5/asset/asset-valuation", parameters)

    member this.GetCandlesticksAsync(instId: string, ?bar: string, ?after: string, ?before: string, ?limit: int) =
        let parameters = Dictionary<string, string>()
        parameters.["instId"] <- instId
        bar |> Option.iter (fun v -> parameters.["bar"] <- v)
        after |> Option.iter (fun v -> parameters.["after"] <- v)
        before |> Option.iter (fun v -> parameters.["before"] <- v)
        limit |> Option.iter (fun v -> parameters.["limit"] <- v.ToString())
        this.SendRequestAsync<OkxCandlestick[]>("GET", "/api/v5/market/candles", parameters)

    member this.PlaceOrderAsync(orderRequest: OkxPlaceOrderRequest) =
        this.SendRequestAsync<OkxPlaceOrderResponse>("POST", "/api/v5/trade/order", body = orderRequest)

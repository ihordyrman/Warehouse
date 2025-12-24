namespace Warehouse.Core.Markets.Concrete.Okx

open System
open System.Net
open System.Net.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Functional.Infrastructure.WebSockets
open Warehouse.Core.Functional.Markets.Concrete.Okx.Services
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Functional.Orders.Contracts
open Warehouse.Core.Markets.Concrete.Okx.Services
open Polly

module DependencyInjection =
    let AddOkxSupport (services: IServiceCollection, configuration: IConfiguration) =
        services.Configure<MarketCredentials>(configuration.GetSection(nameof MarketCredentials)) |> ignore
        services.AddSingleton<IWebSocketClient, WebSocketClient>() |> ignore
        services.AddSingleton<OkxHeartbeatService>() |> ignore
        services.AddScoped<OkxHttpService>() |> ignore
        services.AddScoped<IMarketOrderProvider, OkxMarketOrderProvider>() |> ignore
        services.AddSingleton<OkxMarketAdapter>() |> ignore
        services.AddHostedService<OkxSynchronizationWorker>() |> ignore

        services
            .AddHttpClient(
                "Okx",
                fun (client: HttpClient) ->
                    client.BaseAddress <- Uri("https://www.okx.com/")
                    client.Timeout <- TimeSpan.FromSeconds(30.0)
                    client.DefaultRequestHeaders.Add("User-Agent", "Analyzer/1.0")
            )
            .ConfigurePrimaryHttpMessageHandler(
                Func<HttpMessageHandler>(fun () ->
                    new HttpClientHandler(
                        AutomaticDecompression = (DecompressionMethods.GZip ||| DecompressionMethods.Deflate)
                    ))
            )
            .AddPolicyHandler(fun (provider: IServiceProvider) (_: HttpRequestMessage) ->
                let logger = provider.GetRequiredService<ILogger<HttpClient>>()

                Policy
                    .HandleResult<HttpResponseMessage>(fun (r: HttpResponseMessage) -> not r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(
                        3,
                        (fun retryAttempt -> TimeSpan.FromSeconds(Math.Pow(2.0, float retryAttempt))),
                        (fun result timespan retryCount ctx ->
                            logger.LogWarning(
                                "Retry attempt {RetryCount} after {TimeSpan} seconds",
                                retryCount,
                                timespan
                            ))
                    )
                :> IAsyncPolicy<HttpResponseMessage>)
            .AddPolicyHandler(fun _ ->
                Policy.TimeoutAsync<HttpResponseMessage>(10) :> IAsyncPolicy<HttpResponseMessage>)
        |> ignore

        services

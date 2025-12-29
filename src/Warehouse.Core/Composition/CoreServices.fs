namespace Warehouse.Core

open System
open System.Net
open System.Net.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Polly
open Warehouse.Core.Markets.Services
open Warehouse.Core.Markets.Stores
open Warehouse.Core.Pipelines.Core
open Warehouse.Core.Pipelines.Orchestration
open Warehouse.Core.Pipelines.Trading
open Warehouse.Core.Workers

module CoreServices =

    let private addPipelineOrchestrator (services: IServiceCollection) =
        let registry = TradingSteps.all |> Registry.create

        services.AddSingleton<Registry.T<TradingContext>>(registry) |> ignore

        services.AddHostedService<Orchestrator.Worker>(fun provider ->
            let scopeFactory = provider.GetRequiredService<IServiceScopeFactory>()
            let logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Orchestrator.Worker>>()
            new Orchestrator.Worker(scopeFactory, logger, registry)
        )
        |> ignore

    let AddCoreDependencies (services: IServiceCollection) =
        services.AddSingleton<LiveDataStore.T>(LiveDataStore.create ()) |> ignore

        services.AddHostedService<MarketConnectionService.Worker>(fun provider ->
            let logger = provider.GetRequiredService<ILogger<MarketConnectionService.Worker>>()
            let scopeFactory = provider.GetRequiredService<IServiceScopeFactory>()
            let adapterFactory = CompositionRoot.createAdapterFactory provider
            new MarketConnectionService.Worker(logger, scopeFactory, adapterFactory)
        )
        |> ignore

        addPipelineOrchestrator services

    let AddOkxSupport (services: IServiceCollection, configuration: IConfiguration) =
        services.Configure<MarketCredentials>(configuration.GetSection(nameof MarketCredentials)) |> ignore
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
                    )
                )
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
                            )
                        )
                    )
                :> IAsyncPolicy<HttpResponseMessage>
            )
            .AddPolicyHandler(fun _ ->
                Policy.TimeoutAsync<HttpResponseMessage>(10) :> IAsyncPolicy<HttpResponseMessage>
            )
        |> ignore

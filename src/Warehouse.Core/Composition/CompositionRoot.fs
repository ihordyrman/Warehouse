namespace Warehouse.Core

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Data
open System.Net.Http
open Warehouse.Core.Domain
open Warehouse.Core.Infrastructure
open Warehouse.Core.Markets.Exchanges.Okx
open Warehouse.Core.Markets.Services
open Warehouse.Core.Markets.Stores

module CompositionRoot =
    let createCredentialsStore (services: IServiceProvider) : CredentialsStore.T =
        let serviceScopeFactory = services.GetRequiredService<IServiceScopeFactory>()
        use scope = serviceScopeFactory.CreateScope()
        CredentialsStore.create scope

    let createOkxHttp (services: IServiceProvider) : Http.T =
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OkxHttp")
        let httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("OkxHttpClient")
        let credentialsStore = createCredentialsStore services
        Http.create httpClient credentialsStore logger

    let createBalanceManager (services: IServiceProvider) : BalanceManager.T =
        let loggerFactory = services.GetRequiredService<ILoggerFactory>()
        let okxHttp = createOkxHttp services
        let okxLogger = loggerFactory.CreateLogger("OkxBalanceProvider")
        let okxBalance = BalanceProvider.create okxHttp okxLogger
        BalanceManager.create [ okxBalance ]

    let createCandlestickStore (services: IServiceProvider) : CandlestickStore.T =
        let serviceScopeFactory = services.GetRequiredService<IServiceScopeFactory>()
        use scope = serviceScopeFactory.CreateScope()
        CandlestickStore.create scope

    let createWebSocketClient (services: IServiceProvider) : WebSocketClient.T =
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("WebSocket")
        WebSocketClient.create logger

    let createHeartbeat (services: IServiceProvider) (client: WebSocketClient.T) : Heartbeat.T =
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OkxHeartbeat")
        Heartbeat.create logger client

    let createOrderProviders (services: IServiceProvider) : Warehouse.Core.Markets.Abstractions.OrderExecutor.T list =
        let okxHttp = createOkxHttp services
        let okxLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OkxOrderProvider")

        [ OrderExecutor.create okxHttp okxLogger ]

    let createOrderManager (services: IServiceProvider) : OrdersManager.T =
        let scopeFactory = services.GetRequiredService<IServiceScopeFactory>()
        use scope = scopeFactory.CreateScope()
        let db = scope.ServiceProvider.GetRequiredService<IDbConnection>()
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OrderManager")
        let providers = createOrderProviders services
        OrdersManager.create db providers logger

    let private createOkxAdapter (services: IServiceProvider) : OkxAdapter.T =
        let loggerFactory = services.GetRequiredService<ILoggerFactory>()
        let marketDataCache = services.GetRequiredService<ILiveDataStore>()
        let webSocket = WebSocketClient.create (loggerFactory.CreateLogger("WebSocket"))
        let logger = loggerFactory.CreateLogger("OkxAdapter")
        OkxAdapter.create webSocket marketDataCache logger

    let createAdapterFactory (services: IServiceProvider) : MarketConnectionService.AdapterFactory =
        fun marketType ->
            match marketType with
            | MarketType.Okx -> createOkxAdapter services
            | _ -> failwithf $"Unsupported market type: %A{marketType}"

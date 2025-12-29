namespace Warehouse.Core

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Data
open System.Net.Http
open Warehouse.Core.Infrastructure.WebSockets
open Warehouse.Core.Markets
open Warehouse.Core.Markets.Concrete
open Warehouse.Core.Markets.Concrete.Okx
open Warehouse.Core.Markets.Okx.Services
open Warehouse.Core.Domain
open Warehouse.Core.Markets.Services

module CompositionRoot =
    let createCredentialsStore (services: IServiceProvider) : CredentialsStore.T =
        let serviceScopeFactory = services.GetRequiredService<IServiceScopeFactory>()
        use scope = serviceScopeFactory.CreateScope()
        CredentialsStore.create scope

    let createOkxHttp (services: IServiceProvider) : OkxHttp.T =
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OkxHttp")
        let httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("OkxHttpClient")
        let credentialsStore = createCredentialsStore services
        OkxHttp.create httpClient credentialsStore logger

    let createBalanceManager (services: IServiceProvider) : BalanceManager.T =
        let loggerFactory = services.GetRequiredService<ILoggerFactory>()
        let okxHttp = createOkxHttp services
        let okxLogger = loggerFactory.CreateLogger("OkxBalanceProvider")
        let okxBalance = OkxBalanceOperations.create okxHttp okxLogger
        BalanceManager.create [ okxBalance ]

    let createCandlestickStore (services: IServiceProvider) : CandlestickStore.T =
        let serviceScopeFactory = services.GetRequiredService<IServiceScopeFactory>()
        use scope = serviceScopeFactory.CreateScope()
        CandlestickStore.create scope

    let createWebSocketClient (services: IServiceProvider) : WebSocketClient.T =
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("WebSocket")
        WebSocketClient.create logger

    let createHeartbeat (services: IServiceProvider) (client: WebSocketClient.T) : OkxHeartbeat.T =
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OkxHeartbeat")
        OkxHeartbeat.create logger client

    let createOrderProviders (services: IServiceProvider) : OrderService.T list =
        let okxHttp = createOkxHttp services
        let okxLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OkxOrderProvider")

        [ OkxOrderProvider.create okxHttp okxLogger ]

    let createOrderManager (services: IServiceProvider) : OrdersManager.T =
        let scopeFactory = services.GetRequiredService<IServiceScopeFactory>()
        use scope = scopeFactory.CreateScope()
        let db = scope.ServiceProvider.GetRequiredService<IDbConnection>()
        let logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("OrderManager")
        let providers = createOrderProviders services
        OrdersManager.create db providers logger

    let private createOkxAdapter (services: IServiceProvider) : OkxAdapter.T =
        let loggerFactory = services.GetRequiredService<ILoggerFactory>()
        let marketDataCache = services.GetRequiredService<IMarketDataCache>()
        let webSocket = WebSocketClient.create (loggerFactory.CreateLogger("WebSocket"))
        let logger = loggerFactory.CreateLogger("OkxAdapter")
        OkxAdapter.create webSocket marketDataCache logger

    let createAdapterFactory (services: IServiceProvider) : MarketConnectionService.AdapterFactory =
        fun marketType ->
            match marketType with
            | MarketType.Okx -> createOkxAdapter services
            | _ -> failwithf $"Unsupported market type: %A{marketType}"

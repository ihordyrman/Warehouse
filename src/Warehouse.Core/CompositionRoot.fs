namespace Warehouse.Core

open System
open System.Data
open System.Net.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Infrastructure.WebSockets
open Warehouse.Core.Markets
open Warehouse.Core.Markets.BalanceManager
open Warehouse.Core.Markets.Concrete.Okx
open Warehouse.Core.Markets.Concrete.Okx.Services
open Warehouse.Core.Markets.Domain
open Warehouse.Core.Orders

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
        let okxProvider = OkxBalanceProvider.create okxHttp okxLogger
        BalanceManager.create [ okxProvider ]

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

    let createOrderProviders (services: IServiceProvider) : MarketOrderProvider.T list =
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

namespace Warehouse.Core

open System
open System.Net.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Markets
open Warehouse.Core.Markets.BalanceManager
open Warehouse.Core.Markets.Concrete.Okx
open Warehouse.Core.Markets.Concrete.Okx.Services

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

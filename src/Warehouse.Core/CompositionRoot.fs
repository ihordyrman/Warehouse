namespace Warehouse.Core

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Markets
open Warehouse.Core.Markets.BalanceManager
open Warehouse.Core.Markets.Concrete.Okx.Services

module CompositionRoot =
    let createBalanceManager (services: IServiceProvider) : BalanceManager.T =
        let loggerFactory = services.GetRequiredService<ILoggerFactory>()
        let okxHttpService = services.GetRequiredService<OkxHttpService>()

        let okxLogger = loggerFactory.CreateLogger("OkxBalanceProvider")
        let okxProvider = OkxBalanceProvider.create okxHttpService okxLogger

        BalanceManager.create [ okxProvider ]

    let createCredentialsStore (services: IServiceProvider) : CredentialsStore.T =
        let serviceScopeFactory = services.GetRequiredService<IServiceScopeFactory>()
        use scope = serviceScopeFactory.CreateScope()

        CredentialsStore.create scope

namespace Warehouse.Core.Markets.Stores

open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Infrastructure.Entities
open Warehouse.Core.Shared

module CredentialsStore =
    open Errors

    type Credentials = { Key: string; Secret: string; Passphrase: string option; IsSandbox: bool }

    type T = { GetCredentials: MarketType -> CancellationToken -> Task<Result<Credentials, ServiceError>> }

    let create (scopeFactory: IServiceScopeFactory) : T =
        {
            GetCredentials =
                fun marketType _ ->
                    use scope = scopeFactory.CreateScope()
                    let loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                    let logger = loggerFactory.CreateLogger("CredentialsStore")

                    task {
                        try
                            use scope = scope.ServiceProvider.CreateScope()
                            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()
                            let marketTypeInt = marketType |> int

                            let! results =
                                db.QueryFirstOrDefaultAsync<MarketEntity option>(
                                    "SELECT m.* FROM markets m
                                     WHERE m.type = @Type",
                                    {| Type = marketTypeInt |}
                                )

                            match results with
                            | None ->
                                return Result.Error(NotFound($"No credentials found for market type {marketType}"))
                            | Some credentials ->
                                logger.LogDebug("Retrieved credentials for {MarketType}", credentials.Type)

                                return
                                    Result.Ok(
                                        {
                                            Key = credentials.ApiKey
                                            Secret = credentials.SecretKey
                                            Passphrase =
                                                if credentials.Passphrase = "" then
                                                    None
                                                else
                                                    Some credentials.Passphrase
                                            IsSandbox = credentials.IsSandbox
                                        }
                                    )
                        with ex ->
                            logger.LogError(ex, "Failed to get credentials for {MarketType}", marketType)
                            return Result.Error(NotFound($"Failed to get credentials: {ex.Message}"))
                    }
        }

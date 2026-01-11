namespace Warehouse.Core.Markets.Stores

open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Dapper.FSharp.PostgreSQL
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Infrastructure
open Warehouse.Core.Infrastructure.Entities
open Warehouse.Core.Shared

module CredentialsStore =
    open Errors

    type private MarketEntity = { Id: int; Type: int }

    let private credentialsTable = table'<MarketCredentialsEntity> "market_credentials"
    let private marketsTable = table'<MarketEntity> "markets"

    type T =
        { GetCredentials: MarketType -> CancellationToken -> Task<Result<MarketCredentials, ServiceError>> }

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
                            let marketTypeInt = int marketType

                            let! results =
                                db.QueryAsync<MarketCredentialsEntity>(
                                    "SELECT mc.* FROM markets m
                                 INNER JOIN market_credentials mc ON m.id = mc.market_id
                                 WHERE m.type = @MarketType
                                 LIMIT 1",
                                    {| MarketType = marketTypeInt |}
                                )

                            match results |> Seq.tryHead with
                            | Some credentials ->
                                logger.LogDebug("Retrieved credentials for {MarketType}", marketType)
                                return Ok(Mappers.toMarketCredentials credentials None)
                            | None -> return Result.Error(NotFound($"No credentials found for {marketType}"))
                        with ex ->
                            logger.LogError(ex, "Failed to get credentials for {MarketType}", marketType)
                            return Result.Error(NotFound($"Failed to get credentials: {ex.Message}"))
                    }
        }

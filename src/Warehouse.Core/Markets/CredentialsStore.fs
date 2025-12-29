namespace Warehouse.Core.Markets

open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper.FSharp.PostgreSQL
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Markets.Domain
open Warehouse.Core.Shared

module CredentialsStore =
    open Errors

    type private MarketEntity = { Id: int; Type: int }

    let private credentialsTable = table'<MarketCredentials> "market_credentials"
    let private marketsTable = table'<MarketEntity> "markets"

    type T =
        { GetCredentials: MarketType -> CancellationToken -> Task<Result<MarketCredentials, ServiceError>> }

    let create (scope: IServiceScope) : T =
        {
            GetCredentials =
                fun marketType _ ->
                    let logger = scope.ServiceProvider.GetRequiredService<ILogger>()

                    task {
                        try
                            use scope = scope.ServiceProvider.CreateScope()
                            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()
                            let marketTypeInt = int marketType

                            let! results =
                                select {
                                    for c in credentialsTable do
                                        innerJoin m in marketsTable on (c.MarketId = m.Id)
                                        where (m.Type = marketTypeInt)
                                        take 1
                                }
                                |> db.SelectAsync<MarketCredentials>

                            match results |> Seq.tryHead with
                            | Some credentials ->
                                logger.LogDebug("Retrieved credentials for {MarketType}", marketType)
                                return Ok credentials
                            | None -> return Error(NotFound($"No credentials found for {marketType}"))
                        with ex ->
                            logger.LogError(ex, "Failed to get credentials for {MarketType}", marketType)
                            return Error(NotFound($"Failed to get credentials: {ex.Message}"))
                    }
        }

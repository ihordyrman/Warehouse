namespace Warehouse.Core.Queries

open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Dapper.FSharp.PostgreSQL
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Infrastructure
open Warehouse.Core.Markets.Services

module DashboardQueries =
    open Entities
    open EntityMapping

    let marketsTable = table'<Market> "markets"
    let pipelinesTable = table'<Pipeline> "pipeline_configurations"

    type T =
        {
            CountMarkets: unit -> Task<int>
            CountPipelines: unit -> Task<int>
            CountEnabledPipelines: unit -> Task<int>
            GetAllTags: unit -> Task<string list>
            TotalBalanceUsdt: unit -> Task<decimal>
            ActiveMarkets: unit -> Task<Market list>
        }

    let private queryInt (scopeFactory: IServiceScopeFactory) (sql: string) =
        task {
            use scope = scopeFactory.CreateScope()
            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()
            return! db.QuerySingleAsync<int>(sql)
        }

    let private getTotalBalanceUsdt (scopeFactory: IServiceScopeFactory) =
        task {
            use scope = scopeFactory.CreateScope()
            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()
            let balanceManager = scope.ServiceProvider.GetRequiredService<BalanceManager.T>()
            let! markets = db.QueryAsync<MarketEntity>("SELECT m.* FROM markets m")

            let sum =
                markets
                |> Seq.map (fun market ->
                    task {
                        let! result =
                            (BalanceManager.getTotalUsdtValue
                                balanceManager
                                (enum<MarketType> market.Type)
                                CancellationToken.None)

                        match result with
                        | Ok value -> return value
                        | Result.Error err ->
                            let log = scope.ServiceProvider.GetService<ILogger>()
                            log.LogError("Error getting balance for {MarketType}: {Error}", market.Type, err)
                            return 0M
                    }
                )
                |> Array.ofSeq

            let! results = Task.WhenAll sum
            return results |> Array.sum
        }

    let private getActiveMarkets (scopeFactory: IServiceScopeFactory) : Task<Market list> =
        task {
            use scope = scopeFactory.CreateScope()
            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

            let! results =
                db.QueryAsync<MarketEntity, MarketCredentialsEntity, MarketEntity * MarketCredentialsEntity>(
                    "SELECT m.*, mc.* FROM markets m
                     INNER JOIN market_credentials mc ON m.id = mc.market_id",
                    (fun market creds -> (market, creds)),
                    splitOn = "id"
                )

            return
                results
                |> Seq.map (fun (marketEntity, credsEntity) ->
                    toMarket marketEntity (Some(toMarketCredentials credsEntity None))
                )
                |> Seq.toList
        }

    let private getTags (scopeFactory: IServiceScopeFactory) : Task<string list> =
        task {
            use scope = scopeFactory.CreateScope()
            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

            let! tags =
                db.QueryAsync<string>(
                    "SELECT tags FROM pipeline_configurations
                     GROUP BY tags
                     ORDER BY tags ASC"
                )

            // todo: improve this query
            // since tags are strings, we need additionally parse the result to list
            // and remove duplicates

            return tags |> Seq.toList
        }

    let create (scopeFactory: IServiceScopeFactory) : T =
        {
            CountMarkets = fun () -> queryInt scopeFactory "SELECT COUNT(1) FROM markets"
            CountPipelines = fun () -> queryInt scopeFactory "SELECT COUNT(1) FROM pipeline_configurations"
            CountEnabledPipelines =
                fun () -> queryInt scopeFactory "SELECT COUNT(1) FROM pipeline_configurations WHERE enabled = true"
            GetAllTags = fun () -> getTags scopeFactory
            TotalBalanceUsdt = fun () -> getTotalBalanceUsdt scopeFactory
            ActiveMarkets = fun () -> getActiveMarkets scopeFactory
        }

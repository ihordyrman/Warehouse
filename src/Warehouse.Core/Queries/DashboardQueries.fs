namespace Warehouse.Core.Queries

open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Dapper.FSharp.PostgreSQL
open Microsoft.Extensions.Logging
open Warehouse.Core
open Warehouse.Core.Domain
open Warehouse.Core.Markets.Services

module DashboardQueries =

    let marketsTable = table'<Market> "markets"

    type T =
        {
            CountMarkets: unit -> Task<int>
            CountPipelines: unit -> Task<int>
            CountEnabledPipelines: unit -> Task<int>
            TotalBalanceUsdt: unit -> Task<decimal>
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
            let balanceManager = CompositionRoot.createBalanceManager scope.ServiceProvider

            return
                select {
                    for m in marketsTable do
                        selectAll
                }
                |> db.SelectAsync<Market>
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Seq.map (fun market ->
                    task {
                        let! result =
                            BalanceManager.getTotalUsdtValue balanceManager market.Type CancellationToken.None

                        match result with
                        | Ok value -> return value
                        | Error err ->
                            let log = scope.ServiceProvider.GetService<ILogger>()
                            log.LogError("Error getting balance for {MarketType}: {Error}", market.Type, err)
                            return 0M
                    }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                )
                |> Seq.sum
        }

    let create (scopeFactory: IServiceScopeFactory) : T =
        {
            CountMarkets = fun () -> queryInt scopeFactory "SELECT COUNT(1) FROM markets"
            CountPipelines = fun () -> queryInt scopeFactory "SELECT COUNT(1) FROM pipeline_configurations"
            CountEnabledPipelines =
                fun () -> queryInt scopeFactory "SELECT COUNT(1) FROM pipeline_configurations WHERE enabled = true"
            TotalBalanceUsdt = fun () -> getTotalBalanceUsdt scopeFactory
        }

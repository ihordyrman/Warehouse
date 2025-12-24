namespace Warehouse.Core.Functional.Markets

open System.Threading
open System.Threading.Tasks
open Warehouse.Core.Functional.Markets.Contracts
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Functional.Shared

module BalanceProvider =
    open Errors

    type T =
        {
            MarketType: MarketType
            GetBalances: CancellationToken -> Task<Result<BalanceSnapshot, ServiceError>>
            GetBalance: string -> CancellationToken -> Task<Result<Balance, ServiceError>>
            GetTotalUsdtValue: CancellationToken -> Task<Result<decimal, ServiceError>>
        }

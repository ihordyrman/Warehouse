namespace Warehouse.Core.Markets

open System.Threading
open System.Threading.Tasks
open Warehouse.Core.Domain
open Warehouse.Core.Shared

module BalanceService =
    open Errors

    type T =
        {
            MarketType: MarketType
            GetBalances: CancellationToken -> Task<Result<BalanceSnapshot, ServiceError>>
            GetBalance: string -> CancellationToken -> Task<Result<Balance, ServiceError>>
            GetTotalUsdtValue: CancellationToken -> Task<Result<decimal, ServiceError>>
        }

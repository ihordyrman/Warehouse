namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System
open System.Collections.Generic
open Microsoft.Extensions.Logging
open Warehouse.Core.Functional.Markets.Contracts
open Warehouse.Core.Functional.Markets.Domain
open Warehouse.Core.Functional.Markets.Okx
open Warehouse.Core.Functional.Shared

type OkxBalanceProvider(okxHttpService: OkxHttpService, logger: ILogger<OkxBalanceProvider>) =

    let parseDecimal (value: string) =
        match Decimal.TryParse(value) with
        | true, v -> v
        | false, _ -> 0m

    let mapFundingBalance (funding: OkxFundingBalance) =
        { Currency = funding.Ccy
          Available = parseDecimal funding.AvailBal
          Total = parseDecimal funding.Bal
          Frozen = parseDecimal funding.FrozenBal
          InOrder = 0m
          MarketType = MarketType.Okx
          UpdatedAt = DateTime.UtcNow }

    let mapBalanceDetail (detail: OkxBalanceDetail) =
        { Currency = detail.Ccy
          Available = parseDecimal detail.AvailBal
          Total = parseDecimal detail.CashBal
          Frozen = parseDecimal detail.FrozenBal
          InOrder = parseDecimal detail.OrdFrozen
          MarketType = MarketType.Okx
          UpdatedAt = DateTime.UtcNow }

    let mapAssetBalance (asset: OkxBalanceDetail) =
        { Currency = asset.Ccy
          Available = parseDecimal asset.AvailBal
          Total = parseDecimal asset.Eq
          Frozen = parseDecimal asset.FrozenBal
          InOrder = parseDecimal asset.OrdFrozen
          MarketType = MarketType.Okx
          UpdatedAt = DateTime.UtcNow }

    let mapAccountBalance (okxAccount: OkxAccountBalance) =
        let accountBalance =
            { MarketType = MarketType.Okx
              TotalEquity = parseDecimal okxAccount.TotalEq
              AvailableBalance = parseDecimal okxAccount.AvailEq
              UsedMargin = parseDecimal okxAccount.Imr
              UnrealizedPnl = parseDecimal okxAccount.Upl
              Balances = okxAccount.Details |> List.map mapBalanceDetail |> ResizeArray
              UpdatedAt = DateTime.UtcNow }

        accountBalance

    interface IMarketBalanceProvider with
        member this.MarketType = MarketType.Okx

        member this.GetBalancesAsync(cancellationToken) =
            task {
                try
                    let snapshot =
                        { MarketType = MarketType.Okx
                          Spot = Dictionary<string, Balance>()
                          Funding = Dictionary<string, Balance>()
                          AccountSummary = None
                          Timestamp = DateTime.UtcNow }

                    let! fundingResult = okxHttpService.GetFundingBalanceAsync()

                    if fundingResult.IsSuccess then
                        let balances = fundingResult.Value

                        for funding in balances do
                            let balance = mapFundingBalance funding
                            snapshot.Funding.[balance.Currency] <- balance

                    let! accountResult = okxHttpService.GetAccountBalanceAsync()

                    if accountResult.IsSuccess then
                        let accounts = accountResult.Value

                        if accounts.Length > 0 then
                            let okxAccount = accounts.[0]
                            snapshot.AccountSummary <- Some(mapAccountBalance okxAccount)

                            for detail in okxAccount.Details do
                                let balance = mapBalanceDetail detail
                                snapshot.Spot.[balance.Currency] <- balance

                    let! assetResult = okxHttpService.GetBalanceAsync()

                    if assetResult.IsSuccess then
                        let assets = assetResult.Value

                        for asset in assets do
                            let balance = mapAssetBalance asset

                            if not (snapshot.Spot.ContainsKey(balance.Currency)) then
                                snapshot.Spot.Add(balance.Currency, balance)

                    logger.LogInformation(
                        "Successfully retrieved balance snapshot for OKX with {SpotCount} spot and {FundingCount} funding balances",
                        snapshot.Spot.Count,
                        snapshot.Funding.Count
                    )

                    return Result<BalanceSnapshot>.Success(snapshot)
                with ex ->
                    logger.LogError(ex, "Failed to get balance snapshot from OKX")
                    return Result<BalanceSnapshot>.Failure(Error($"Failed to get balances: {ex.Message}"))
            }

        member this.GetBalanceAsync(currency, cancellationToken) =
            task {
                try
                    let! result = okxHttpService.GetBalanceAsync(currency)

                    if not result.IsSuccess then
                        return Result<Balance>.Failure(result.Error)
                    else
                        let balances = result.Value

                        match balances |> Array.tryFind _.Ccy.Equals(currency, StringComparison.OrdinalIgnoreCase) with
                        | None -> return Result<Balance>.Failure(Error($"Currency {currency} not found"))
                        | Some okxBalance -> return Result<Balance>.Success(mapAssetBalance okxBalance)
                with ex ->
                    logger.LogError(ex, "Failed to get balance for currency {Currency} from OKX", currency)
                    return Result<Balance>.Failure(Error($"Failed to get balance: {ex.Message}"))
            }

        member this.GetTotalUsdtValueAsync(cancellationToken) =
            task {
                try
                    let! portfolioResult = okxHttpService.GetAssetsValuationAsync()

                    if portfolioResult.IsSuccess then
                        let valuations = portfolioResult.Value
                        let value = valuations |> Array.sumBy (fun x -> parseDecimal x.TotalBalance)
                        logger.LogInformation("Successfully retrieved total portfolio value: {Value} USDT", value)
                        return Result<decimal>.Success(value)
                    else
                        logger.LogWarning("Failed to get total portfolio value from OKX: {Error}", portfolioResult.Error.Message)
                        return Result<decimal>.Failure(Error("Failed to get total USDT value"))
                with ex ->
                    logger.LogError(ex, "Failed to get total USDT value")
                    return Result<decimal>.Failure(Error($"Failed to get total USDT value: {ex.Message}"))
            }

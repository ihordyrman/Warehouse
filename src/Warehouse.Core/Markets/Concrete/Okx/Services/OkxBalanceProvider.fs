namespace Warehouse.Core.Markets.Concrete.Okx.Services

module OkxBalanceProvider =
    open Microsoft.Extensions.Logging
    open System
    open System.Threading
    open Warehouse.Core.Functional.Markets
    open Warehouse.Core.Functional.Markets.Contracts
    open Warehouse.Core.Functional.Markets.Domain
    open Warehouse.Core.Functional.Markets.Okx
    open Warehouse.Core.Functional.Shared.Errors

    let private parseDecimal (value: string) =
        match Decimal.TryParse(value) with
        | true, x -> x
        | false, _ -> 0m

    let private mapFundingBalance (funding: OkxFundingBalance) : Balance =
        {
            Currency = funding.Ccy
            Available = parseDecimal funding.AvailBal
            Total = parseDecimal funding.Bal
            Frozen = parseDecimal funding.FrozenBal
            InOrder = 0m
            MarketType = MarketType.Okx
            UpdatedAt = DateTime.UtcNow
        }

    let private mapBalanceDetail (detail: OkxBalanceDetail) : Balance =
        {
            Currency = detail.Ccy
            Available = parseDecimal detail.AvailBal
            Total = parseDecimal detail.CashBal
            Frozen = parseDecimal detail.FrozenBal
            InOrder = parseDecimal detail.OrdFrozen
            MarketType = MarketType.Okx
            UpdatedAt = DateTime.UtcNow
        }

    let private mapAccountBalance (okxAccount: OkxAccountBalance) : AccountBalance =
        {
            MarketType = MarketType.Okx
            TotalEquity = parseDecimal okxAccount.TotalEq
            AvailableBalance = parseDecimal okxAccount.AvailEq
            UsedMargin = parseDecimal okxAccount.Imr
            UnrealizedPnl = parseDecimal okxAccount.Upl
            Balances = okxAccount.Details |> List.map mapBalanceDetail
            UpdatedAt = DateTime.UtcNow
        }

    let create (httpService: OkxHttpService) (logger: ILogger) : BalanceProvider.T =

        let getBalances (ct: CancellationToken) =
            task {
                try
                    let mutable snapshot =
                        {
                            MarketType = MarketType.Okx
                            Timestamp = DateTime.UtcNow
                            Spot = Map.empty
                            Funding = Map.empty
                            AccountSummary = None
                        }

                    let! fundingResult = httpService.GetFundingBalanceAsync()

                    match fundingResult with
                    | Ok balances ->
                        let funding =
                            balances |> Array.map mapFundingBalance |> Array.map (fun b -> b.Currency, b) |> Map.ofArray

                        snapshot <- { snapshot with Funding = funding }
                    | Error _ -> ()

                    let! accountResult = httpService.GetAccountBalanceAsync()

                    match accountResult with
                    | Ok [| okxAccount |] ->
                        let spot =
                            okxAccount.Details
                            |> List.map mapBalanceDetail
                            |> List.map (fun b -> b.Currency, b)
                            |> Map.ofList

                        snapshot <- { snapshot with Spot = spot; AccountSummary = Some(mapAccountBalance okxAccount) }
                    | _ -> ()

                    logger.LogInformation(
                        "Retrieved OKX snapshot: {SpotCount} spot, {FundingCount} funding",
                        snapshot.Spot.Count,
                        snapshot.Funding.Count
                    )

                    return Ok snapshot
                with ex ->
                    logger.LogError(ex, "Failed to get OKX balance snapshot")
                    return Error(Unexpected ex)
            }

        let getBalance (currency: string) (ct: CancellationToken) =
            task {
                try
                    let! result = httpService.GetBalanceAsync(currency)

                    match result with
                    | Ok balances ->
                        match balances |> Array.tryFind _.Ccy.Equals(currency, StringComparison.OrdinalIgnoreCase) with
                        | Some detail -> return Ok(mapBalanceDetail detail)
                        | None -> return Error(NotFound $"Currency {currency}")
                    | Error err -> return Error err
                with ex ->
                    return Error(Unexpected ex)
            }

        let getTotalUsdtValue (ct: CancellationToken) =
            task {
                try
                    let! result = httpService.GetAssetsValuationAsync()

                    match result with
                    | Ok valuations ->
                        let total = valuations |> Array.sumBy (fun v -> parseDecimal v.TotalBalance)
                        return Ok total
                    | Error err -> return Error err
                with ex ->
                    return Error(Unexpected ex)
            }

        {
            MarketType = MarketType.Okx
            GetBalances = getBalances
            GetBalance = getBalance
            GetTotalUsdtValue = getTotalUsdtValue
        }

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Warehouse.Core.Old.Functional.Markets.Contracts;
using Warehouse.Core.Old.Functional.Markets.Domain;
using Warehouse.Core.Old.Functional.Shared;

namespace Warehouse.App.Old.Pages;

public class BalanceModel(IBalanceManager balanceManager) : PageModel
{
    public decimal Available { get; set; }

    public decimal InOrders { get; set; }

    public decimal Total { get; set; }

    public async Task<IActionResult> OnGetAsync(string marketType)
    {
        try
        {
            if (!Enum.TryParse(marketType, true, out MarketType market))
            {
                return Page();
            }

            Result<AccountBalance> result = await balanceManager.GetAccountBalanceAsync(market, CancellationToken.None);
            if (result.IsSuccess)
            {
                Available = result.Value.AvailableBalance;
                Total = result.Value.TotalEquity;
                InOrders = result.Value.UsedMargin;
                return Page();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ServiceError loading balance for {marketType}: {ex.Message}");
        }

        return Page();
    }

    private class TotalUsdtResponse
    {
        public decimal TotalUsdtValue { get; set; }
    }
}

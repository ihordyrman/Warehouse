using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Shared;

namespace Warehouse.App.Pages;

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

            Result<AccountBalance> result = await balanceManager.GetAccountBalanceAsync(market);
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
            Console.WriteLine($"Error loading balance for {marketType}: {ex.Message}");
        }

        return Page();
    }

    private class TotalUsdtResponse
    {
        public decimal TotalUsdtValue { get; set; }
    }
}

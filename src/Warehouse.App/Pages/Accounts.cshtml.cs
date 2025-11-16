using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Warehouse.Core.Markets.Contracts;

namespace Warehouse.App.Pages;

public class AccountsModel(IBalanceManager balanceManager, ILogger<AccountsModel> logger) : PageModel
{
    public List<AccountDetailsViewModel> Accounts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // var marketsResult = await marketService.GetAllMarketsAsync();
            // var accountsResult = await marketService.GetAllAccountsAsync();
            //
            // if (!marketsResult.IsSuccess)
            // {
            //     return Page();
            // }
            //
            // foreach (var market in marketsResult.Value)
            // {
            //     var account = accountsResult.Value?.FirstOrDefault(a => a.MarketId == market.Id);
            //
            //     var accountVm = new AccountDetailsViewModel
            //     {
            //         Id = market.Id,
            //         Name = market.Name,
            //         Type = market.Type.ToString(),
            //         Enabled = market.Enabled,
            //         HasCredentials = account?.HasCredentials ?? false,
            //         Balance = 0,
            //         UpdatedAt = market.UpdatedAt
            //     };
            //
            //     // Fetch balance if enabled and has credentials
            //     if (market.Enabled && accountVm.HasCredentials)
            //     {
            //         try
            //         {
            //             var balanceResult = await balanceManager.GetTotalUsdtValueAsync(market.Type);
            //             if (balanceResult.IsSuccess)
            //             {
            //                 accountVm.Balance = balanceResult.Value;
            //             }
            //         }
            //         catch (Exception ex)
            //         {
            //             logger.LogWarning(ex, "Failed to fetch balance for {MarketName}", market.Name);
            //         }
            //     }
            //
            //     Accounts.Add(accountVm);
            // }

            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load accounts");
            TempData["Error"] = "Failed to load accounts. Please try again.";
            return Page();
        }
    }
}

public class AccountDetailsViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public bool HasCredentials { get; set; }

    public decimal Balance { get; set; }

    public DateTime UpdatedAt { get; set; }
}

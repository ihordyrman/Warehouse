using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Old.Functional.Infrastructure.Persistence;
using Warehouse.Core.Old.Functional.Markets.Contracts;
using Warehouse.Core.Old.Functional.Markets.Domain;
using Warehouse.Core.Old.Functional.Shared;

namespace Warehouse.App.Old.Pages;

[IgnoreAntiforgeryToken]
public class AccountsModel(WarehouseDbContext db, IBalanceManager balanceManager, ILogger<AccountsModel> logger) : PageModel
{
    public List<AccountDetailsViewModel> Accounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string FilterType { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string FilterStatus { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAccountsAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_AccountList", Accounts);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, bool enabled)
    {
        Market? market = await db.Markets.Include(x => x.Credentials).FirstOrDefaultAsync(x => x.Id == id);

        if (market is { Credentials: not null })
        {
            // ?
        }

        await LoadAccountsAsync();
        return Partial("_AccountList", Accounts);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        Market? market = await db.Markets.Include(x => x.Credentials).FirstOrDefaultAsync(x => x.Id == id);

        if (market != null)
        {
            db.Markets.Remove(market);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await LoadAccountsAsync();
        return Partial("_AccountList", Accounts);
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            IQueryable<Market> query = db.Markets.AsNoTracking().Include(x => x.Credentials).AsQueryable();

            if (!string.IsNullOrEmpty(FilterType))
            {
                if (Enum.TryParse(FilterType, true, out MarketType typeEnum))
                {
                    query = query.Where(x => x.Type == typeEnum);
                }
            }

            List<Market> markets = await query.ToListAsync();

            Accounts = new List<AccountDetailsViewModel>();

            foreach (Market market in markets)
            {
                var vm = new AccountDetailsViewModel
                {
                    Id = market.Id,
                    Name = market.Type.ToString(),
                    Type = market.Type.ToString(),
                    Enabled = market.Credentials != null,
                    HasCredentials = market.Credentials != null,
                    Balance = 0,
                    UpdatedAt = market.UpdatedAt
                };

                if (!string.IsNullOrEmpty(SearchTerm) && !vm.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(FilterStatus))
                {
                    bool isEnabled = vm.Enabled;
                    if (FilterStatus == "enabled" && !isEnabled)
                    {
                        continue;
                    }

                    if (FilterStatus == "disabled" && isEnabled)
                    {
                        continue;
                    }
                }

                if (vm.HasCredentials)
                {
                    try
                    {
                        Result<decimal> balanceResult = await balanceManager.GetTotalUsdtValueAsync(market.Type, CancellationToken.None);
                        if (balanceResult.IsSuccess)
                        {
                            vm.Balance = balanceResult.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to fetch balance for {MarketName}", market.Type);
                    }
                }

                Accounts.Add(vm);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load accounts");
            Accounts = new List<AccountDetailsViewModel>();
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

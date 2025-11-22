using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared;

namespace Warehouse.App.Pages;

[IgnoreAntiforgeryToken]
public class DashboardModel(IBalanceManager balanceManager, WarehouseDbContext db, IConfiguration configuration) : PageModel
{
    public List<MarketInfo> Markets { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public int ActiveAccountsCount { get; set; }

    public int RunningWorkersCount { get; set; }

    public decimal TotalBalance { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            await LoadAccountsAsync();

            ActiveAccountsCount = Markets.Count(x => x.Enabled);
            RunningWorkersCount = await db.WorkerDetails.CountAsync();
            Tags = await db.WorkerDetails
                .SelectMany(x => x.Tags)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading dashboard data: {ex.Message}");
        }

        return Page();
    }

    private async Task LoadAccountsAsync()
    {
        List<MarketAccount> accounts = await db.MarketAccounts.Include(x => x.MarketDetails).ToListAsync();

        foreach (MarketAccount account in accounts)
        {
            var marketInfo = new MarketInfo
            {
                Id = account.MarketDetails.Id,
                Name = account.MarketDetails.Type.ToString(),
                Type = account.MarketDetails.Type.ToString(),
                Enabled = true,
                HasCredentials = true
            };

            Markets.Add(marketInfo);
            Result<decimal> totalUsdt = await balanceManager.GetTotalUsdtValueAsync(account.MarketDetails.Type);
            if (totalUsdt.IsSuccess)
            {
                TotalBalance += totalUsdt.Value;
            }
        }
    }

    public class MarketInfo
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        public bool Enabled { get; set; }

        public bool HasCredentials { get; set; }
    }
}

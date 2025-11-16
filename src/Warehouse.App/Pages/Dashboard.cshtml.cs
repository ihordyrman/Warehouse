using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.App.Endpoints.Models;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.App.Pages;

public class DashboardModel(IBalanceManager balanceManager, WarehouseDbContext db, IConfiguration configuration) : PageModel
{
    public List<MarketInfo> Markets { get; set; } = [];

    public List<WorkerInfo> Workers { get; set; } = [];

    public int ActiveAccountsCount { get; set; }

    public int RunningWorkersCount { get; set; }

    public decimal TotalBalance { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
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

            // Fetch workers
            int workers = await db.WorkerDetails.CountAsync();

            // Calculate totals
            ActiveAccountsCount = Markets.Count(m => m.Enabled);
            RunningWorkersCount = workers;
        }
        catch (Exception ex)
        {
            // Log error but don't fail the page
            Console.WriteLine($"Error loading dashboard data: {ex.Message}");
        }

        return Page();
    }

    public class MarketInfo
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        public bool Enabled { get; set; }

        public bool HasCredentials { get; set; }
    }

    public class WorkerInfo
    {
        public int Id { get; set; }

        public string Symbol { get; set; } = "";

        public string MarketType { get; set; } = "";

        public bool Enabled { get; set; }

        public string? Strategy { get; set; }

        public string? Interval { get; set; }

        public DateTime? LastRun { get; set; }
    }

    private class MarketDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        public bool Enabled { get; set; }
    }

    private class AccountDto
    {
        public int Id { get; set; }

        public int MarketId { get; set; }

        public bool HasCredentials { get; set; }
    }

    private class TotalUsdtResponse
    {
        public decimal TotalUsdtValue { get; set; }
    }
}

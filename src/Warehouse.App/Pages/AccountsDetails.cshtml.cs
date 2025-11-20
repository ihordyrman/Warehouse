using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared;

namespace Warehouse.App.Pages;

public class AccountsDetailsModel(WarehouseDbContext db, IBalanceManager balanceManager) : PageModel
{
    public AccountDetailViewModel Account { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        MarketDetails? market = await db.MarketDetails.Include(x => x.Credentials).FirstOrDefaultAsync(x => x.Id == id);

        if (market == null)
        {
            return NotFound();
        }

        Account = new AccountDetailViewModel
        {
            Id = market.Id,
            Name = market.Type.ToString(),
            Type = market.Type.ToString(),
            Enabled = market.Credentials != null,
            HasCredentials = market.Credentials != null,
            Balance = 0,
            UpdatedAt = market.UpdatedAt
        };

        if (Account.HasCredentials)
        {
            Result<decimal> balanceResult = await balanceManager.GetTotalUsdtValueAsync(market.Type);
            if (balanceResult.IsSuccess)
            {
                Account.Balance = balanceResult.Value;
            }
        }

        return Page();
    }

    public class AccountDetailViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public bool Enabled { get; set; }

        public bool HasCredentials { get; set; }

        public decimal Balance { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}

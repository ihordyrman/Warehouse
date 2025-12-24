using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Old.Functional.Infrastructure.Persistence;
using Warehouse.Core.Old.Functional.Markets.Domain;

namespace Warehouse.App.Old.Pages;

public class AccountsCreateModel(WarehouseDbContext db) : PageModel
{
    [BindProperty]
    public CreateAccountInput Input { get; set; } = new();

    public List<MarketType> MarketTypes { get; set; } = [];

    public void OnGet() => MarketTypes = Enum.GetValues<MarketType>().ToList();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            MarketTypes = Enum.GetValues<MarketType>().ToList();
            return Page();
        }

        Market? market = await db.Markets.Include(x => x.Credentials).FirstOrDefaultAsync(x => x.Type == Input.Type);

        if (market == null)
        {
            market = new Market
            {
                Type = Input.Type,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Markets.Add(market);
        }
        else if (market.Credentials != null)
        {
            ModelState.AddModelError("Input.Type", $"Account for {Input.Type} already exists. Please edit the existing account.");
            MarketTypes = Enum.GetValues<MarketType>().ToList();
            return Page();
        }

        var account = new MarketCredentials
        {
            Market = market,
            ApiKey = Input.ApiKey,
            SecretKey = Input.SecretKey,
            Passphrase = Input.Passphrase ?? string.Empty,
            IsSandbox = Input.IsSandbox,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MarketCredentials.Add(account);
        await db.SaveChangesAsync(CancellationToken.None);

        return RedirectToPage("/Accounts");
    }

    public class CreateAccountInput
    {
        [Required]
        public MarketType Type { get; set; }

        [Required]
        [Display(Name = "API Key")]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Secret Key")]
        public string SecretKey { get; set; } = string.Empty;

        [Display(Name = "Passphrase")]
        public string? Passphrase { get; set; }

        [Display(Name = "Sandbox")]
        public bool IsSandbox { get; set; }
    }
}

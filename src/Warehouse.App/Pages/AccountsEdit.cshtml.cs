using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;

namespace Warehouse.App.Pages;

public class AccountsEditModel(WarehouseDbContext db) : PageModel
{
    [BindProperty]
    public EditAccountInput Input { get; set; } = new();

    public string MarketName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        MarketDetails? market = await db.MarketDetails.Include(x => x.Credentials).FirstOrDefaultAsync(x => x.Id == id);

        if (market == null)
        {
            return NotFound();
        }

        MarketName = market.Type.ToString();

        Input = new EditAccountInput
        {
            Id = market.Id,
            ApiKey = market.Credentials?.ApiKey ?? string.Empty,

            SecretKey = market.Credentials?.SecretKey ?? string.Empty,
            Passphrase = market.Credentials?.Passphrase,
            Enabled = market.Credentials != null,
            IsSandbox = market.Credentials?.IsSandbox ?? false
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        MarketDetails? market = await db.MarketDetails.Include(x => x.Credentials).FirstOrDefaultAsync(x => x.Id == Input.Id);

        if (market == null)
        {
            return NotFound();
        }

        MarketName = market.Type.ToString();

        if (Input.Enabled)
        {
            if (market.Credentials == null)
            {
                var account = new MarketAccount
                {
                    MarketDetails = market,
                    ApiKey = Input.ApiKey,
                    SecretKey = Input.SecretKey,
                    Passphrase = Input.Passphrase ?? string.Empty,
                    IsSandbox = Input.IsSandbox,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.MarketAccounts.Add(account);
            }
            else
            {
                market.Credentials.ApiKey = Input.ApiKey;
                market.Credentials.SecretKey = Input.SecretKey;
                market.Credentials.Passphrase = Input.Passphrase ?? string.Empty;
                market.Credentials.IsSandbox = Input.IsSandbox;
                market.Credentials.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            if (market.Credentials != null)
            {
                market.Credentials.ApiKey = Input.ApiKey;
                market.Credentials.SecretKey = Input.SecretKey;
                market.Credentials.Passphrase = Input.Passphrase ?? string.Empty;
                market.Credentials.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var account = new MarketAccount
                {
                    MarketDetails = market,
                    ApiKey = Input.ApiKey,
                    SecretKey = Input.SecretKey,
                    Passphrase = Input.Passphrase ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.MarketAccounts.Add(account);
            }
        }

        await db.SaveChangesAsync();

        return RedirectToPage("/Accounts");
    }

    public class EditAccountInput
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "API Key")]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Secret Key")]
        public string SecretKey { get; set; } = string.Empty;

        [Display(Name = "Passphrase")]
        public string? Passphrase { get; set; }

        public bool Enabled { get; set; }

        [Display(Name = "Sandbox")]
        public bool IsSandbox { get; set; }
    }
}

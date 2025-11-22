using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.App.Pages;

public class WorkersCreateModel(WarehouseDbContext db) : PageModel
{
    [BindProperty]
    public CreateWorkerInput Input { get; set; } = new();

    public List<MarketType> MarketTypes { get; set; } = [];

    public void OnGet() => MarketTypes = Enum.GetValues<MarketType>().ToList();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            MarketTypes = Enum.GetValues<MarketType>().ToList();
            return Page();
        }

        string symbolUpper = Input.Symbol.ToUpperInvariant();
        bool exists = await db.WorkerDetails.AnyAsync(x => x.Type == Input.Type && x.Symbol == symbolUpper);

        if (exists)
        {
            ModelState.AddModelError("Input.Symbol", $"Worker for {Input.Type}/{Input.Symbol} already exists");
            MarketTypes = Enum.GetValues<MarketType>().ToList();
            return Page();
        }

        List<string> tags = string.IsNullOrWhiteSpace(Input.TagsInput) ? [] :
            Input.TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

        var worker = new WorkerDetails
        {
            Type = Input.Type,
            Symbol = symbolUpper,
            Enabled = Input.Enabled,
            Tags = tags
        };

        db.WorkerDetails.Add(worker);
        await db.SaveChangesAsync();

        return RedirectToPage("/Dashboard");
    }

    public class CreateWorkerInput
    {
        public bool Enabled { get; set; } = false;

        public MarketType Type { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string TagsInput { get; set; } = string.Empty;
    }
}

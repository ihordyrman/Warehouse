using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.App.Pages;

public class WorkersEditModel(WarehouseDbContext db) : PageModel
{
    [BindProperty]
    public EditWorkerInput Input { get; set; } = new();

    public List<MarketType> MarketTypes { get; set; } = [];

    public int WorkerId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        WorkerId = id;
        MarketTypes = Enum.GetValues<MarketType>().ToList();

        WorkerDetails? worker = await db.WorkerDetails.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (worker == null)
        {
            return NotFound();
        }

        Input = new EditWorkerInput
        {
            Enabled = worker.Enabled,
            Type = worker.Type,
            Symbol = worker.Symbol,
            TagsInput = string.Join(", ", worker.Tags)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        WorkerId = id;

        if (!ModelState.IsValid)
        {
            MarketTypes = Enum.GetValues<MarketType>().ToList();
            return Page();
        }

        WorkerDetails? worker = await db.WorkerDetails.FirstOrDefaultAsync(x => x.Id == id);

        if (worker == null)
        {
            return NotFound();
        }

        string symbolUpper = Input.Symbol.ToUpperInvariant();
        if (worker.Type != Input.Type || worker.Symbol != symbolUpper)
        {
            bool exists = await db.WorkerDetails.AnyAsync(x => x.Id != id && x.Type == Input.Type && x.Symbol == symbolUpper);

            if (exists)
            {
                ModelState.AddModelError("Input.Symbol", $"Worker for {Input.Type}/{Input.Symbol} already exists");
                MarketTypes = Enum.GetValues<MarketType>().ToList();
                return Page();
            }
        }

        List<string> tags = string.IsNullOrWhiteSpace(Input.TagsInput) ? [] :
            Input.TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

        worker.Enabled = Input.Enabled;
        worker.Type = Input.Type;
        worker.Symbol = symbolUpper;
        worker.Tags = tags;

        await db.SaveChangesAsync();

        return RedirectToPage("/Workers");
    }

    public class EditWorkerInput
    {
        public bool Enabled { get; set; }

        public MarketType Type { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string TagsInput { get; set; } = string.Empty;
    }
}

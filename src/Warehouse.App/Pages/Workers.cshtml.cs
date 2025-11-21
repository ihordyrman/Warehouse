using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.App.Pages;

[IgnoreAntiforgeryToken]
public class WorkersModel(WarehouseDbContext db) : PageModel
{
    public List<WorkerInfo> Workers { get; set; } = [];

    public List<string> AllTags { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string FilterTag { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string FilterAccount { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string FilterStatus { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "symbol";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadWorkersAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_WorkerListRows", Workers);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync([FromQuery] int id, [FromQuery] bool enabled)
    {
        WorkerDetails? worker = await db.WorkerDetails.FirstOrDefaultAsync(x => x.Id == id);

        if (worker == null)
        {
            return NotFound();
        }

        worker.Enabled = enabled;
        await db.SaveChangesAsync();

        var updatedWorker = new WorkerInfo
        {
            Id = worker.Id,
            Symbol = worker.Symbol,
            MarketType = worker.Type.ToString(),
            Enabled = worker.Enabled,
            Tags = worker.Tags,
            LastExecutedAt = worker.UpdatedAt
        };

        return Partial("_WorkerListRow", updatedWorker);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        WorkerDetails? worker = await db.WorkerDetails.FirstOrDefaultAsync(x => x.Id == id);

        if (worker != null)
        {
            db.WorkerDetails.Remove(worker);
            await db.SaveChangesAsync();
        }

        await LoadWorkersAsync();
        return Partial("_WorkerListRows", Workers);
    }

    private async Task LoadWorkersAsync()
    {
        IQueryable<WorkerDetails> query = db.WorkerDetails.AsNoTracking();

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            query = query.Where(x => x.Symbol.Contains(SearchTerm));
        }

        if (!string.IsNullOrEmpty(FilterTag))
        {
            query = query.Where(x => x.Tags.Contains(FilterTag));
        }

        if (!string.IsNullOrEmpty(FilterAccount))
        {
            query = query.Where(x => x.Type.ToString() == FilterAccount);
        }

        if (!string.IsNullOrEmpty(FilterStatus))
        {
            bool isEnabled = FilterStatus == "enabled";
            query = query.Where(x => x.Enabled == isEnabled);
        }

        List<WorkerDetails> workers = await query.ToListAsync();

        workers = SortBy switch
        {
            "symbol-desc" => workers.OrderByDescending(x => x.Symbol).ToList(),
            "account" => workers.OrderBy(x => x.Type).ToList(),
            "account-desc" => workers.OrderByDescending(x => x.Type).ToList(),
            "status" => workers.OrderBy(x => x.Enabled).ToList(),
            "status-desc" => workers.OrderByDescending(x => x.Enabled).ToList(),
            "updated" => workers.OrderBy(x => x.UpdatedAt).ToList(),
            "updated-desc" => workers.OrderByDescending(x => x.UpdatedAt).ToList(),
            _ => workers.OrderBy(x => x.Symbol).ToList()
        };

        Workers = workers.Select(x => new WorkerInfo
            {
                Id = x.Id,
                Symbol = x.Symbol,
                MarketType = x.Type.ToString(),
                Enabled = x.Enabled,
                Tags = x.Tags,
                LastExecutedAt = x.UpdatedAt
            })
            .ToList();

        AllTags = await db.WorkerDetails.AsNoTracking().SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToListAsync();
    }

    public class WorkerInfo
    {
        public int Id { get; set; }

        public string Symbol { get; set; } = "";

        public string MarketType { get; set; } = "";

        public bool Enabled { get; set; }

        public List<string> Tags { get; set; } = [];

        public DateTime? LastExecutedAt { get; set; }
    }
}

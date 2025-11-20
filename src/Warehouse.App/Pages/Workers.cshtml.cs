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

    public bool OnlyEnabled { get; set; }

    public async Task OnGetAsync(bool onlyEnabled = false)
    {
        OnlyEnabled = onlyEnabled;

        IQueryable<WorkerDetails> query = db.WorkerDetails.AsNoTracking();

        if (onlyEnabled)
        {
            query = query.Where(x => x.Enabled);
        }

        List<WorkerDetails> workers = await query.ToListAsync();

        Workers = workers.Select(x => new WorkerInfo
            {
                Id = x.Id,
                Symbol = x.Symbol,
                MarketType = x.Type.ToString(),
                Enabled = x.Enabled,
                Strategy = null, // TODO: Add strategy info when available
                Interval = null, // TODO: Add interval info when available
                LastRun = null,  // TODO: Add last run info when available
                LastExecutedAt = x.UpdatedAt
            })
            .ToList();
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
            Strategy = null,
            Interval = null,
            LastRun = null,
            LastExecutedAt = worker.UpdatedAt
        };

        return Partial("_WorkerCard", updatedWorker);
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

        public DateTime? LastExecutedAt { get; set; }
    }
}

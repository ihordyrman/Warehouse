using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.App.Pages;

public class ToggleModel(WarehouseDbContext db) : PageModel
{
    public WorkerInfo? Worker { get; set; }

    public async Task<IActionResult> OnPostAsync(int id, [FromForm] bool enabled)
    {
        WorkerDetails? worker = await db.WorkerDetails.FirstOrDefaultAsync(x => x.Id == id);

        if (worker == null)
        {
            return NotFound();
        }

        worker.Enabled = enabled;
        await db.SaveChangesAsync();

        Worker = new WorkerInfo
        {
            Id = worker.Id,
            Symbol = worker.Symbol,
            MarketType = worker.Type.ToString(),
            Enabled = worker.Enabled,
            Strategy = null, // TODO: Add strategy info when available
            Interval = null, // TODO: Add interval info when available
            LastRun = null,
            LastExecutedAt = worker.UpdatedAt
        };

        return Page();
    }

    public class WorkerInfo
    {
        public int Id { get; set; }

        public string Symbol { get; set; } = "";

        public string MarketType { get; set; } = "";

        public bool Enabled { get; set; }

        public string? Strategy { get; set; }

        public string? StrategyName { get; set; }

        public string? Interval { get; set; }

        public DateTime? LastRun { get; set; }

        public DateTime? LastExecutedAt { get; set; }
    }
}

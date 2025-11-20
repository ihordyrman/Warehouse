using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.App.Pages;

public class WorkersDetailsModel(WarehouseDbContext db) : PageModel
{
    public WorkerDetailsInfo? Worker { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        WorkerDetails? worker = await db.WorkerDetails.AsNoTracking().Include(x => x.PipelineSteps).FirstOrDefaultAsync(x => x.Id == id);

        if (worker == null)
        {
            return NotFound();
        }

        Worker = new WorkerDetailsInfo
        {
            Id = worker.Id,
            Enabled = worker.Enabled,
            Type = worker.Type,
            Symbol = worker.Symbol,
            CreatedAt = worker.CreatedAt,
            UpdatedAt = worker.UpdatedAt,
            PipelineStepsCount = worker.PipelineSteps.Count
        };

        return Page();
    }

    public class WorkerDetailsInfo
    {
        public int Id { get; set; }

        public bool Enabled { get; set; }

        public MarketType Type { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int PipelineStepsCount { get; set; }
    }
}

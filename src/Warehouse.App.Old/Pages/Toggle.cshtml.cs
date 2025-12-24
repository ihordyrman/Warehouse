using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Old.Functional.Infrastructure.Persistence;
using Warehouse.Core.Old.Functional.Pipelines.Domain;

namespace Warehouse.App.Old.Pages;

public class ToggleModel(WarehouseDbContext db) : PageModel
{
    public PipelineInfo? Pipeline { get; set; }

    public async Task<IActionResult> OnPostAsync(int id, [FromForm] bool enabled)
    {
        Pipeline? pipeline = await db.PipelineConfigurations.FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        pipeline.Enabled = enabled;
        await db.SaveChangesAsync(CancellationToken.None);

        Pipeline = new PipelineInfo
        {
            Id = pipeline.Id,
            Symbol = pipeline.Symbol,
            MarketType = pipeline.MarketType.ToString(),
            Enabled = pipeline.Enabled,
            Strategy = null, // TODO: Add strategy info when available
            Interval = pipeline.ExecutionInterval.ToString(),
            LastRun = pipeline.LastExecutedAt,
            LastExecutedAt = pipeline.UpdatedAt
        };

        return Page();
    }

    public class PipelineInfo
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

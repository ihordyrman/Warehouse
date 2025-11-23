using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.App.Pages;

public class PipelinesDetailsModel(WarehouseDbContext db) : PageModel
{
    public PipelineDetailsInfo? Pipeline { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Pipeline? pipeline = await db.PipelineConfigurations.AsNoTracking().Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        Pipeline = new PipelineDetailsInfo
        {
            Id = pipeline.Id,
            Enabled = pipeline.Enabled,
            Type = pipeline.MarketType,
            Symbol = pipeline.Symbol,
            CreatedAt = pipeline.CreatedAt,
            UpdatedAt = pipeline.UpdatedAt,
            PipelineStepsCount = pipeline.Steps.Count
        };

        return Page();
    }

    public class PipelineDetailsInfo
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

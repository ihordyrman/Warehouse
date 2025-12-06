using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.App.Pages;

[IgnoreAntiforgeryToken]
public class PipelinesModel(WarehouseDbContext db) : PageModel
{
    public List<PipelineInfo> Pipelines { get; set; } = [];

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
        await LoadPipelinesAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_PipelineListRows", Pipelines);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync([FromQuery] int id, [FromQuery] bool enabled)
    {
        Pipeline? pipeline = await db.PipelineConfigurations.FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        pipeline.Enabled = enabled;
        await db.SaveChangesAsync();

        var updatedPipeline = new PipelineInfo
        {
            Id = pipeline.Id,
            Symbol = pipeline.Symbol,
            MarketType = pipeline.MarketType.ToString(),
            Enabled = pipeline.Enabled,
            Tags = pipeline.Tags,
            LastExecutedAt = pipeline.UpdatedAt
        };

        return Partial("_PipelineListRow", updatedPipeline);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        Pipeline? pipeline = await db.PipelineConfigurations.FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline != null)
        {
            db.PipelineConfigurations.Remove(pipeline);
            await db.SaveChangesAsync();
        }

        await LoadPipelinesAsync();
        return Partial("_PipelineListRows", Pipelines);
    }

    private async Task LoadPipelinesAsync()
    {
        IQueryable<Pipeline> query = db.PipelineConfigurations.AsNoTracking();

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            query = query.Where(x => x.Symbol.Contains(SearchTerm));
        }



        if (!string.IsNullOrEmpty(FilterAccount))
        {
            query = query.Where(x => x.MarketType.ToString() == FilterAccount);
        }

        if (!string.IsNullOrEmpty(FilterStatus))
        {
            bool isEnabled = FilterStatus == "enabled";
            query = query.Where(x => x.Enabled == isEnabled);
        }

        List<Pipeline> pipelines = await query.ToListAsync();

        if (!string.IsNullOrEmpty(FilterTag))
        {
            pipelines = pipelines.Where(x => x.Tags.Contains(FilterTag)).ToList();
        }

        pipelines = SortBy switch
        {
            "symbol-desc" => pipelines.OrderByDescending(x => x.Symbol).ToList(),
            "account" => pipelines.OrderBy(x => x.MarketType).ToList(),
            "account-desc" => pipelines.OrderByDescending(x => x.MarketType).ToList(),
            "status" => pipelines.OrderBy(x => x.Enabled).ToList(),
            "status-desc" => pipelines.OrderByDescending(x => x.Enabled).ToList(),
            "updated" => pipelines.OrderBy(x => x.UpdatedAt).ToList(),
            "updated-desc" => pipelines.OrderByDescending(x => x.UpdatedAt).ToList(),
            _ => pipelines.OrderBy(x => x.Symbol).ToList()
        };

        Pipelines = pipelines.Select(x => new PipelineInfo
            {
                Id = x.Id,
                Symbol = x.Symbol,
                MarketType = x.MarketType.ToString(),
                Enabled = x.Enabled,
                Tags = x.Tags,
                LastExecutedAt = x.UpdatedAt
            })
            .ToList();

        var allTags = await db.PipelineConfigurations.AsNoTracking().Select(x => x.Tags).ToListAsync();
        AllTags = allTags.SelectMany(x => x).Distinct().OrderBy(x => x).ToList();
    }

    public class PipelineInfo
    {
        public int Id { get; set; }

        public string Symbol { get; set; } = "";

        public string MarketType { get; set; } = "";

        public bool Enabled { get; set; }

        public List<string> Tags { get; set; } = [];

        public DateTime? LastExecutedAt { get; set; }
    }
}

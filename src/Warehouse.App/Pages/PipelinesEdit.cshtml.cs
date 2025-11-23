using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.App.Pages;

public class PipelinesEditModel(WarehouseDbContext db) : PageModel
{
    [BindProperty]
    public EditPipelineInput Input { get; set; } = new();

    public List<MarketType> MarketTypes { get; set; } = [];

    public int PipelineId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        PipelineId = id;
        MarketTypes = Enum.GetValues<MarketType>().ToList();

        Pipeline? pipeline = await db.PipelineConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        Input = new EditPipelineInput
        {
            Enabled = pipeline.Enabled,
            Type = pipeline.MarketType,
            Symbol = pipeline.Symbol,
            TagsInput = string.Join(", ", pipeline.Tags)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        PipelineId = id;

        if (!ModelState.IsValid)
        {
            MarketTypes = Enum.GetValues<MarketType>().ToList();
            return Page();
        }

        Pipeline? pipeline = await db.PipelineConfigurations.FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        string symbolUpper = Input.Symbol.ToUpperInvariant();
        if (pipeline.MarketType != Input.Type || pipeline.Symbol != symbolUpper)
        {
            bool exists = await db.PipelineConfigurations.AnyAsync(x => x.Id != id && x.MarketType == Input.Type && x.Symbol == symbolUpper);

            if (exists)
            {
                ModelState.AddModelError("Input.Symbol", $"Pipeline for {Input.Type}/{Input.Symbol} already exists");
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

        pipeline.Enabled = Input.Enabled;
        pipeline.MarketType = Input.Type;
        pipeline.Symbol = symbolUpper;
        pipeline.Tags = tags;

        await db.SaveChangesAsync();

        return RedirectToPage("/Dashboard");
    }

    public class EditPipelineInput
    {
        public bool Enabled { get; set; }

        public MarketType Type { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string TagsInput { get; set; } = string.Empty;
    }
}

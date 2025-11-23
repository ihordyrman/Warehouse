using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.App.Pages;

public class PipelinesCreateModel(WarehouseDbContext db) : PageModel
{
    [BindProperty]
    public CreatePipelineInput Input { get; set; } = new();

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
        bool exists = await db.PipelineConfigurations.AnyAsync(x => x.MarketType == Input.Type && x.Symbol == symbolUpper);

        if (exists)
        {
            ModelState.AddModelError("Input.Symbol", $"Pipeline for {Input.Type}/{Input.Symbol} already exists");
            MarketTypes = Enum.GetValues<MarketType>().ToList();
            return Page();
        }

        List<string> tags = string.IsNullOrWhiteSpace(Input.TagsInput) ? [] :
            Input.TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

        var pipeline = new Pipeline
        {
            MarketType = Input.Type,
            Symbol = symbolUpper,
            Enabled = Input.Enabled,
            Tags = tags
        };

        db.PipelineConfigurations.Add(pipeline);
        await db.SaveChangesAsync();

        return RedirectToPage("/Dashboard");
    }

    public class CreatePipelineInput
    {
        public bool Enabled { get; set; } = false;

        public MarketType Type { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string TagsInput { get; set; } = string.Empty;
    }
}

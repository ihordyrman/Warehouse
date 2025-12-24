using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.App.Old.Pages.Models;
using Warehouse.Core.Old.Functional.Infrastructure.Persistence;
using Warehouse.Core.Old.Functional.Markets.Domain;
using Warehouse.Core.Old.Functional.Pipelines.Domain;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Registry;
using Warehouse.Core.Old.Pipelines.Steps;

namespace Warehouse.App.Old.Pages;

[IgnoreAntiforgeryToken]
public class PipelinesEditModel(WarehouseDbContext db, IStepRegistry stepRegistry) : PageModel
{
    [BindProperty]
    public EditPipelineInput Input { get; set; } = new();

    public List<MarketType> MarketTypes { get; set; } = [];

    public int PipelineId { get; set; }

    public List<StepItemViewModel> Steps { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        PipelineId = id;
        MarketTypes = Enum.GetValues<MarketType>().ToList();

        Pipeline? pipeline = await db.PipelineConfigurations.Include(x => x.Steps).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        Input = new EditPipelineInput
        {
            Enabled = pipeline.Enabled,
            Type = pipeline.MarketType,
            Symbol = pipeline.Symbol,
            TagsInput = string.Join(", ", pipeline.Tags),
            ExecutionInterval = (int)pipeline.ExecutionInterval.TotalMinutes
        };

        LoadSteps(pipeline);

        return Page();
    }

    public async Task<IActionResult> OnGetStepListAsync(int id)
    {
        Pipeline? pipeline = await db.PipelineConfigurations.Include(x => x.Steps).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        LoadSteps(pipeline);

        return Partial(
            "Shared/_StepList",
            new StepListViewModel
            {
                PipelineId = id,
                Steps = Steps
            });
    }

    public async Task<IActionResult> OnGetStepSelectorAsync(int id)
    {
        Pipeline? pipeline = await db.PipelineConfigurations.Include(p => p.Steps).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (pipeline == null)
        {
            return NotFound();
        }

        var existingKeys = pipeline.Steps.Select(s => s.StepTypeKey).ToHashSet();

        var definitions = stepRegistry.GetAllDefinitions()
            .Select(d => new StepDefinitionViewModel
            {
                Key = d.Key,
                Name = d.Name,
                Description = d.Description,
                Category = d.Category.ToString(),
                Icon = d.Icon,
                IsAlreadyInPipeline = existingKeys.Contains(d.Key)
            })
            .ToList();

        return Partial(
            "Shared/_StepSelector",
            new StepSelectorViewModel
            {
                PipelineId = id,
                Definitions = definitions
            });
    }

    public async Task<IActionResult> OnGetStepEditorAsync(int id, int stepId)
    {
        PipelineStep? step = await db.PipelineSteps.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stepId && s.PipelineDetailsId == id);

        if (step == null)
        {
            return NotFound();
        }

        IStepDefinition? definition = stepRegistry.GetDefinition(step.StepTypeKey);
        if (definition == null)
        {
            return NotFound();
        }

        ParameterSchema schema = definition.GetParameterSchema();
        StepEditorViewModel vm = BuildStepEditorViewModel(id, stepId, definition, schema, step.Parameters);

        return Partial("Shared/_StepEditor", vm);
    }

    public async Task<IActionResult> OnPostAddStepAsync(int id, string stepTypeKey)
    {
        IStepDefinition? definition = stepRegistry.GetDefinition(stepTypeKey);
        if (definition == null)
        {
            return BadRequest("Unknown step type");
        }

        int maxOrder = await db.PipelineSteps.Where(x => x.PipelineDetailsId == id).MaxAsync(s => (int?)s.Order) ?? 0;

        ParameterSchema schema = definition.GetParameterSchema();
        Dictionary<string, string> defaultParams = schema.GetDefaultValues();

        var step = new PipelineStep
        {
            PipelineDetailsId = id,
            StepTypeKey = stepTypeKey,
            Name = definition.Name,
            Order = maxOrder + 1,
            IsEnabled = true,
            Parameters = defaultParams
        };

        db.PipelineSteps.Add(step);
        await db.SaveChangesAsync(CancellationToken.None);

        StepItemViewModel vm = MapToStepItemViewModel(step, definition, id);
        vm.IsFirst = maxOrder == 0;
        vm.IsLast = true;

        return Partial("Shared/_StepItem", vm);
    }

    public async Task<IActionResult> OnPostSaveStepAsync(int id, int stepId)
    {
        PipelineStep? step = await db.PipelineSteps.FirstOrDefaultAsync(s => s.Id == stepId);
        if (step == null)
        {
            return NotFound();
        }

        IStepDefinition? definition = stepRegistry.GetDefinition(step.StepTypeKey);
        if (definition == null)
        {
            return BadRequest("Unknown step type");
        }

        ParameterSchema schema = definition.GetParameterSchema();
        var newParams = new Dictionary<string, string>();

        if (Request.HasFormContentType)
        {
            foreach (ParameterDefinition param in schema.Parameters)
            {
                string? value = Request.Form[param.Key].FirstOrDefault();

                if (param.Type == ParameterType.Boolean)
                {
                    newParams[param.Key] = (value == "true").ToString().ToLowerInvariant();
                }
                else if (value != null)
                {
                    newParams[param.Key] = value;
                }
            }
        }
        else
        {
            newParams = new Dictionary<string, string>(step.Parameters);
        }

        ValidationResult validationResult = stepRegistry.ValidateParameters(step.StepTypeKey, newParams);
        if (!validationResult.IsValid)
        {
            StepEditorViewModel vm = BuildStepEditorViewModel(id, stepId, definition, schema, newParams);
            vm.Errors = validationResult.Errors.Select(e => e.Message).ToList();
            return Partial("Shared/_StepEditor", vm);
        }

        step.Parameters = newParams;
        await db.SaveChangesAsync(CancellationToken.None);

        int minOrder = await db.PipelineSteps.Where(x => x.PipelineDetailsId == id).MinAsync(x => (int?)x.Order) ?? 0;
        int maxOrder = await db.PipelineSteps.Where(x => x.PipelineDetailsId == id).MaxAsync(x => (int?)x.Order) ?? 0;

        StepItemViewModel itemVm = MapToStepItemViewModel(step, definition, id);
        itemVm.IsFirst = step.Order == minOrder;
        itemVm.IsLast = step.Order == maxOrder;

        return Partial("Shared/_StepItem", itemVm);
    }

    public async Task<IActionResult> OnPostToggleStepAsync([FromQuery] int id, [FromQuery] int stepId)
    {
        PipelineStep? step = await db.PipelineSteps.FirstOrDefaultAsync(s => s.Id == stepId);
        if (step == null)
        {
            return NotFound();
        }

        step.IsEnabled = !step.IsEnabled;
        await db.SaveChangesAsync(CancellationToken.None);

        IStepDefinition? definition = stepRegistry.GetDefinition(step.StepTypeKey);
        StepItemViewModel vm = MapToStepItemViewModel(step, definition, id);

        return Partial("Shared/_StepItem", vm);
    }

    public async Task<IActionResult> OnDeleteDeleteStepAsync([FromQuery] int id, [FromQuery] int stepId)
    {
        PipelineStep? step = await db.PipelineSteps.FirstOrDefaultAsync(s => s.Id == stepId);
        if (step == null)
        {
            return new EmptyResult();
        }

        db.PipelineSteps.Remove(step);
        await db.SaveChangesAsync(CancellationToken.None);

        return new EmptyResult();
    }

    public async Task<IActionResult> OnPostMoveStepAsync([FromQuery] int id, [FromQuery] int stepId, [FromQuery] string direction)
    {
        List<PipelineStep> steps = await db.PipelineSteps.Where(x => x.PipelineDetailsId == id).OrderBy(x => x.Order).ToListAsync();
        PipelineStep? currentStep = steps.FirstOrDefault(x => x.Id == stepId);

        if (currentStep == null)
        {
            return NotFound();
        }

        int index = steps.IndexOf(currentStep);

        PipelineStep? targetStep = direction switch
        {
            "up" when index > 0 => steps[index - 1],
            "down" when index < steps.Count - 1 => steps[index + 1],
            _ => null
        };

        if (targetStep != null)
        {
            int originalCurrentOrder = currentStep.Order;
            int originalTargetOrder = targetStep.Order;

            currentStep.Order = -1;
            await db.SaveChangesAsync(CancellationToken.None);

            targetStep.Order = originalCurrentOrder;
            await db.SaveChangesAsync(CancellationToken.None);

            currentStep.Order = originalTargetOrder;
            await db.SaveChangesAsync(CancellationToken.None);
        }

        Pipeline? pipeline = await db.PipelineConfigurations.Include(x => x.Steps).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (pipeline == null)
        {
            return NotFound();
        }

        LoadSteps(pipeline);

        return Partial("Shared/_StepList", new StepListViewModel { PipelineId = id, Steps = Steps });
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
            bool exists =
                await db.PipelineConfigurations.AnyAsync(x => x.Id != id && x.MarketType == Input.Type && x.Symbol == symbolUpper);

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
        pipeline.ExecutionInterval = TimeSpan.FromMinutes(Input.ExecutionInterval);

        await db.SaveChangesAsync(CancellationToken.None);

        return RedirectToPage("/Pipelines");
    }

    private void LoadSteps(Pipeline pipeline)
    {
        var sortedSteps = pipeline.Steps.OrderBy(s => s.Order).ToList();
        Steps = sortedSteps.Select((s, i) =>
            {
                IStepDefinition? definition = stepRegistry.GetDefinition(s.StepTypeKey);
                StepItemViewModel vm = MapToStepItemViewModel(s, definition, pipeline.Id);
                vm.IsFirst = i == 0;
                vm.IsLast = i == sortedSteps.Count - 1;
                return vm;
            })
            .ToList();
    }

    private static StepItemViewModel MapToStepItemViewModel(PipelineStep step, IStepDefinition? definition, int pipelineId)
    {
        string paramSummary = step.Parameters.Count > 0 ?
            string.Join(", ", step.Parameters.Take(3).Select(p => $"{p.Key}: {p.Value}")) :
            "";

        return new StepItemViewModel
        {
            Id = step.Id,
            PipelineId = pipelineId,
            StepTypeKey = step.StepTypeKey,
            DisplayName = definition?.Name ?? step.Name,
            Description = definition?.Description ?? "",
            Icon = definition?.Icon ?? "fa-puzzle-piece",
            Category = definition?.Category.ToString() ?? "Unknown",
            Order = step.Order,
            IsEnabled = step.IsEnabled,
            ParameterSummary = paramSummary
        };
    }

    private static StepEditorViewModel BuildStepEditorViewModel(
        int pipelineId,
        int stepId,
        IStepDefinition definition,
        ParameterSchema schema,
        IDictionary<string, string> currentValues)
    {
        var fields = schema.Parameters.Select(p => new ParameterFieldViewModel
            {
                Key = p.Key,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Type = p.Type,
                IsRequired = p.IsRequired,
                CurrentValue = currentValues.TryGetValue(p.Key, out string? v) ? v : null,
                DefaultValue = p.DefaultValue,
                Group = p.Group,
                Order = p.Order,
                Min = p.Min,
                Max = p.Max,
                Options = p.Options?.Select(o => new SelectOptionViewModel { Value = o.Value, Label = o.Label }).ToList()
            })
            .ToList();

        return new StepEditorViewModel
        {
            PipelineId = pipelineId,
            StepId = stepId,
            StepTypeKey = definition.Key,
            StepName = definition.Name,
            StepDescription = definition.Description,
            StepIcon = definition.Icon,
            Fields = fields
        };
    }

    public class EditPipelineInput
    {
        public bool Enabled { get; set; }

        public MarketType Type { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string TagsInput { get; set; } = string.Empty;

        public int ExecutionInterval { get; set; } = 1;
    }
}

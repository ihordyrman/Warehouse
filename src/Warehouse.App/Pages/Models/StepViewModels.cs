using Warehouse.Core.Pipelines.Parameters;

namespace Warehouse.App.Pages.Models;

/// <summary>
///     View model for displaying a step in the list.
/// </summary>
public class StepItemViewModel
{
    public int Id { get; set; }

    public int PipelineId { get; set; }

    public string StepTypeKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsEnabled { get; set; }

    public string ParameterSummary { get; set; } = string.Empty;
}

/// <summary>
///     View model for step list container.
/// </summary>
public class StepListViewModel
{
    public int PipelineId { get; set; }

    public List<StepItemViewModel> Steps { get; set; } = [];
}

/// <summary>
///     View model for selecting a step type to add.
/// </summary>
public class StepDefinitionViewModel
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public bool IsAlreadyInPipeline { get; set; }
}

/// <summary>
///     View model for step selector modal.
/// </summary>
public class StepSelectorViewModel
{
    public int PipelineId { get; set; }

    public List<StepDefinitionViewModel> Definitions { get; set; } = [];
}

/// <summary>
///     View model for step parameter editor.
/// </summary>
public class StepEditorViewModel
{
    public int PipelineId { get; set; }

    public int? StepId { get; set; }

    public string StepTypeKey { get; set; } = string.Empty;

    public string StepName { get; set; } = string.Empty;

    public string StepDescription { get; set; } = string.Empty;

    public string StepIcon { get; set; } = string.Empty;

    public List<ParameterFieldViewModel> Fields { get; set; } = [];

    public List<string> Errors { get; set; } = [];
}

/// <summary>
///     View model for a single parameter field.
/// </summary>
public class ParameterFieldViewModel
{
    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ParameterType Type { get; set; }

    public bool IsRequired { get; set; }

    public string? CurrentValue { get; set; }

    public string? DefaultValue { get; set; }

    public string? Group { get; set; }

    public int Order { get; set; }

    public decimal? Min { get; set; }

    public decimal? Max { get; set; }

    public List<SelectOptionViewModel>? Options { get; set; }
}

/// <summary>
///     View model for select options.
/// </summary>
public class SelectOptionViewModel
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

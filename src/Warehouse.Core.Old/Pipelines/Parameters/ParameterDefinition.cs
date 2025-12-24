namespace Warehouse.Core.Old.Pipelines.Parameters;

/// <summary>
///     Defines a single parameter for a pipeline step.
/// </summary>
public record ParameterDefinition
{
    /// <summary>
    ///     The unique key for this parameter (used in the dictionary).
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    ///     The display name shown in the UI.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Description/help text for this parameter.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     The data type of this parameter.
    /// </summary>
    public required ParameterType Type { get; init; }

    /// <summary>
    ///     Whether this parameter is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    ///     The default value as a string.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    ///     Optional grouping for organizing parameters in the UI.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    ///     Display order within the group.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    ///     Minimum value for numeric types.
    /// </summary>
    public decimal? Min { get; init; }

    /// <summary>
    ///     Maximum value for numeric types.
    /// </summary>
    public decimal? Max { get; init; }

    /// <summary>
    ///     Options for Select type parameters.
    /// </summary>
    public IReadOnlyList<SelectOption>? Options { get; init; }
}

/// <summary>
///     An option for Select type parameters.
/// </summary>
public record SelectOption(string Value, string Label);

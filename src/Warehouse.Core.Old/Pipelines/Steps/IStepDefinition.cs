using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Trading;

namespace Warehouse.Core.Old.Pipelines.Steps;

/// <summary>
///     Defines metadata and factory for a pipeline step type.
/// </summary>
public interface IStepDefinition
{
    /// <summary>
    ///     Unique key identifying this step type (e.g., "take-profit").
    /// </summary>
    string Key { get; }

    /// <summary>
    ///     Display name for the UI (e.g., "Take Profit").
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Description of what this step does.
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     Category for UI organization.
    /// </summary>
    StepCategory Category { get; }

    /// <summary>
    ///     FontAwesome icon class (e.g., "fa-trending-up").
    /// </summary>
    string Icon { get; }

    /// <summary>
    ///     Gets the parameter schema for this step.
    /// </summary>
    ParameterSchema GetParameterSchema();

    /// <summary>
    ///     Creates an instance of the step with the given parameters.
    /// </summary>
    IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters);
}

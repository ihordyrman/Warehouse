using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Trading;

namespace Warehouse.Core.Old.Pipelines.Steps;

/// <summary>
///     Abstract base class for step definitions with common functionality.
/// </summary>
public abstract class BaseStepDefinition : IStepDefinition
{
    /// <inheritdoc/>
    public abstract string Key { get; }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public abstract StepCategory Category { get; }

    /// <inheritdoc/>
    public virtual string Icon => GetDefaultIcon();

    /// <inheritdoc/>
    public abstract ParameterSchema GetParameterSchema();

    /// <inheritdoc/>
    public abstract IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters);

    private string GetDefaultIcon()
        => Category switch
        {
            StepCategory.Validation => "fa-check-circle",
            StepCategory.Risk => "fa-shield-alt",
            StepCategory.Signal => "fa-chart-line",
            StepCategory.Execution => "fa-play-circle",
            _ => "fa-puzzle-piece"
        };
}

using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Steps;
using Warehouse.Core.Old.Pipelines.Trading;

namespace Warehouse.Core.Old.Pipelines.Registry;

/// <summary>
///     Registry for discovering and creating pipeline step instances.
/// </summary>
public interface IStepRegistry
{
    /// <summary>
    ///     Gets all registered step definitions.
    /// </summary>
    IReadOnlyList<IStepDefinition> GetAllDefinitions();

    /// <summary>
    ///     Gets a step definition by its key.
    /// </summary>
    IStepDefinition? GetDefinition(string key);

    /// <summary>
    ///     Gets all step definitions in a category.
    /// </summary>
    IEnumerable<IStepDefinition> GetByCategory(StepCategory category);

    /// <summary>
    ///     Creates a step instance from a definition key and parameters.
    /// </summary>
    IPipelineStep<TradingContext> CreateInstance(string key, IServiceProvider services, ParameterBag parameters);

    /// <summary>
    ///     Validates parameters against the schema for a step type.
    /// </summary>
    ValidationResult ValidateParameters(string key, IReadOnlyDictionary<string, string> parameters);
}

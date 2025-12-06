using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Parameters;
using Warehouse.Core.Pipelines.Trading;

namespace Warehouse.Core.Pipelines.Builder;

/// <summary>
///     Builds pipeline step instances from database configuration.
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    ///     Creates step instances for a pipeline based on its configuration.
    /// </summary>
    IReadOnlyList<IPipelineStep<TradingContext>> BuildSteps(Pipeline pipeline, IServiceProvider services);

    /// <summary>
    ///     Validates a pipeline configuration before execution.
    /// </summary>
    ValidationResult ValidatePipeline(Pipeline pipeline);
}

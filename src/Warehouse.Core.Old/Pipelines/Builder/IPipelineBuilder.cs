using Warehouse.Core.Old.Functional.Pipelines.Domain;
using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Trading;

namespace Warehouse.Core.Old.Pipelines.Builder;

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

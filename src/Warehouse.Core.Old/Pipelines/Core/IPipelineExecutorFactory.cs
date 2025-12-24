using Warehouse.Core.Old.Functional.Pipelines.Domain;

namespace Warehouse.Core.Old.Pipelines.Core;

/// <summary>
///     Factory for creating pipeline executor instances.
///     Abstracts the direct instantiation to support unit testing.
/// </summary>
public interface IPipelineExecutorFactory
{
    /// <summary>
    ///     Creates a new executor instance for the given pipeline.
    /// </summary>
    IPipelineExecutor Create(Pipeline pipeline, IServiceProvider serviceProvider);
}

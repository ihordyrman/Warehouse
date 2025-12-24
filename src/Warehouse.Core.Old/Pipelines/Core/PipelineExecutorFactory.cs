using Warehouse.Core.Old.Functional.Pipelines.Domain;

namespace Warehouse.Core.Old.Pipelines.Core;

public class PipelineExecutorFactory : IPipelineExecutorFactory
{
    public IPipelineExecutor Create(Pipeline pipeline, IServiceProvider serviceProvider) => new PipelineExecutor(serviceProvider, pipeline);
}

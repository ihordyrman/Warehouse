using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.Core.Pipelines.Core;

public class PipelineExecutorFactory : IPipelineExecutorFactory
{
    public IPipelineExecutor Create(Pipeline pipeline, IServiceProvider serviceProvider) => new PipelineExecutor(serviceProvider, pipeline);
}

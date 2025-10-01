using Warehouse.Core.Models.Pipelines;

namespace Warehouse.Core.Abstractions.Pipelines;

public interface IPipelineBuilder<TContext>
    where TContext : IPipelineContext
{
    IPipelineBuilder<TContext> AddStep(IPipelineStep<TContext> step);

    IPipelineBuilder<TContext> AddSteps(params IPipelineStep<TContext>[] steps);

    IPipelineBuilder<TContext> OnError(Func<TContext, Exception, Task> errorHandler);

    IPipelineBuilder<TContext> OnComplete(Func<TContext, PipelineResult, Task> completionHandler);

    IPipeline<TContext> Build();
}

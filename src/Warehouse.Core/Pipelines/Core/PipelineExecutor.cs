using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Trading;

namespace Warehouse.Core.Pipelines.Core;

public class PipelineExecutor : IPipelineExecutor
{
    private readonly IServiceProvider serviceProvider;
    private readonly Pipeline configuration;
    private CancellationTokenSource cts;

    public int PipelineId { get; }

    public bool IsRunning { get; }

    public Task StartAsync(CancellationToken ct)
    {
        cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = Task.Run(() => ExecuteLoopAsync(cts.Token), ct);

        return Task.CompletedTask;
    }

    public Task StopAsync() => throw new NotImplementedException();

    private async Task ExecuteLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            var context = new TradingContext
            {
                PipelineId = 0,
                Symbol = configuration.Symbol
            };

            foreach (PipelineStep step in configuration.Steps.OrderBy(s => s.Order))
            {
                // var stepInstance = CreateStep(step, scope.ServiceProvider);
                // var result = await stepInstance.ExecuteAsync(context, ct);
                //
                // if (!result.ShouldContinue)
                // {
                //     break;
                // }
            }

            await Task.Delay(configuration.ExecutionInterval, ct);
        }
    }
}

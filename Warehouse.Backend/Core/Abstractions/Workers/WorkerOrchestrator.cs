using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Core.Abstractions.Workers;

public class WorkerOrchestrator(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly Dictionary<int, (IMarketWorker Worker, CancellationTokenSource Cts)> workers = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

            // Start workers that are not yet started
            // Stop workers that should be stopped
            // Adjust configuration for each worker if needed
        }
    }
}

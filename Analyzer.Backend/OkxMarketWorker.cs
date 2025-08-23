using Analyzer.Backend.Okx;

namespace Analyzer.Backend;

public class OkxMarketWorker(ILogger<OkxMarketWorker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scopeFactory = serviceScopeFactory.CreateAsyncScope();
        OkxWsClient okxWsClient = scopeFactory.ServiceProvider.GetService<OkxWsClient>()!;

        okxWsClient.OnLoginSuccess += () =>
        {
            _ = okxWsClient.SubscribeToChannelAsync("books", "OKB-USDT");
        };

        okxWsClient.OnDataReceived += data =>
        {
            logger.LogInformation("Data received {Data}", data);
        };

        await okxWsClient.ConnectAsync(OkxChannelType.Public);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}

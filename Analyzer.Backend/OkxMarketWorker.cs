using Analyzer.Backend.Okx;
using Analyzer.Backend.Okx.Processors;

namespace Analyzer.Backend;

public class OkxMarketWorker(
    OkxRawMessageProcessor messageProcessor,
    ILogger<OkxMarketWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scopeFactory = serviceScopeFactory.CreateAsyncScope();
        OkxWsClient okxWsClient = scopeFactory.ServiceProvider.GetService<OkxWsClient>()!;
        await okxWsClient.ConnectAsync(OkxChannelType.Public);
        await okxWsClient.SubscribeToChannelAsync("books", "OKB-USDT");
        await messageProcessor.StartProcessingAsync(stoppingToken);
    }
}

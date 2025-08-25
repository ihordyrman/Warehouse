using Analyzer.Backend.Okx.Handlers;
using Analyzer.Backend.Okx.Messages.Socket;
using Analyzer.Backend.Okx.Services;

namespace Analyzer.Backend.Okx.Processors;

public class OkxMessageProcessor(
    OkxWebSocketService webSocketService,
    IEnumerable<IOkxMessageHandler> handlers,
    ILogger<OkxMessageProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (OkxSocketResponse message in webSocketService.GetMessagesAsync(stoppingToken))
        {
            try
            {
                foreach (IOkxMessageHandler handler in handlers)
                {
                    if (await handler.CanHandleAsync(message))
                    {
                        await handler.HandleAsync(message, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");
            }
        }
    }
}

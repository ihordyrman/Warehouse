using Warehouse.Backend.Markets.Okx.Handlers;
using Warehouse.Backend.Markets.Okx.Messages.Socket;
using Warehouse.Backend.Markets.Okx.Services;

namespace Warehouse.Backend.Markets.Okx.Processors;

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

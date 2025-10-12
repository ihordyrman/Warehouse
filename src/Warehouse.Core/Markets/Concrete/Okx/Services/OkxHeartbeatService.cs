using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.WebSockets;

namespace Warehouse.Core.Markets.Concrete.Okx.Services;

public class OkxHeartbeatService(ILogger<OkxHeartbeatService> logger) : IDisposable
{
    private readonly PeriodicTimer timer = new(TimeSpan.FromSeconds(10));
    private CancellationTokenSource? cts;
    private Task? heartbeatTask;

    public void Dispose()
    {
        Stop();
        timer.Dispose();
    }

    public void Start(IWebSocketClient client)
    {
        Stop();

        cts = new CancellationTokenSource();
        heartbeatTask = Task.Run(
            async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await timer.WaitForNextTickAsync(cts.Token);
                        await client.SendAsync("ping", cts.Token);
                        logger.LogDebug("Heartbeat sent");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send heartbeat");
                    }
                }
            },
            cts.Token);
    }

    public void Stop()
    {
        cts?.Cancel();
        heartbeatTask?.Wait(TimeSpan.FromSeconds(5));
        cts?.Dispose();
        cts = null;
        heartbeatTask = null;
    }
}

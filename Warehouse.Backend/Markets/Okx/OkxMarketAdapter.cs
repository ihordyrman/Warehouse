using Warehouse.Backend.Core.Abstractions;
using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Markets.Okx.Services;

namespace Warehouse.Backend.Markets.Okx;

public class OkxMarketAdapter(
    OkxWebSocketService wsService,
    OkxHttpService httpService,
    ILogger<OkxMarketAdapter> logger) : IOkxMarketAdapter
{
    public static MarketType Type => MarketType.Okx;

    public bool IsConnected => wsService.IsConnected;

    public IMarketDataProvider DataProvider { get; private set; }

    public IOrderExecutor OrderExecutor { get; private set; }

    public IAccountManager AccountManager { get; private set; }

    public async Task<bool> ConnectAsync(MarketCredentials credentials)
    {
        try
        {
            // httpService.Configure(credentials);
            // await wsService.ConnectAsync(credentials);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to OKX");
            return false;
        }
    }
}

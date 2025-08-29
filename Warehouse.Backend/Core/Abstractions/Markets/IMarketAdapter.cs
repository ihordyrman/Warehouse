using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Abstractions.Markets;

public interface IOkxMarketAdapter
{
    static abstract MarketType Type { get; }

    bool IsConnected { get; }

    IMarketDataProvider DataProvider { get; }

    IOrderExecutor OrderExecutor { get; }

    IAccountManager AccountManager { get; }

    Task<bool> ConnectAsync(MarketCredentials credentials);
}

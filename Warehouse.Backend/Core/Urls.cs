using Warehouse.Backend.Core.Entities;

namespace Warehouse.Backend.Core;

public static class Urls
{
    public static Uri GetMarketUrl(Market market)
        => market switch
        {
            Market.Okx => new Uri("https://www.okx.com/"),
            _ => throw new ArgumentOutOfRangeException(nameof(market), market, null)
        };
}

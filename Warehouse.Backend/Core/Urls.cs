using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core;

using static MarketType;

public static class Urls
{
    public static Uri GetMarketUrl(MarketType marketType)
        => marketType switch
        {
            Okx => new Uri("https://www.okx.com/"),
            _ => throw new ArgumentOutOfRangeException(nameof(marketType), marketType, null)
        };
}

namespace Warehouse.Backend.Features.Okx.Constants;

// ReSharper disable InconsistentNaming

// https://www.okx.com/docs-v5/en/#overview-account-mode
internal static class SocketEndpoints
{
    internal const string PUBLIC_WS_URL = "wss://ws.okx.com:8443/ws/v5/public";
    internal const string PRIVATE_WS_URL = "wss://ws.okx.com:8443/ws/v5/private";
    internal const string BUSINESS_WS_URL = "wss://ws.okx.com:8443/ws/v5/business";

    internal const string DEMO_PUBLIC_WS_URL = "wss://wspap.okx.com:8443/ws/v5/public";
    internal const string DEMO_PRIVATE_WS_URL = "wss://wspap.okx.com:8443/ws/v5/private";
    internal const string DEMO_BUSINESS_WS_URL = "wss://wspap.okx.com:8443/ws/v5/business";
}

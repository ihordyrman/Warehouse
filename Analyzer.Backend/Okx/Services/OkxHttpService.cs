using System.Collections.Specialized;
using System.Text.Json;
using System.Web;
using Analyzer.Backend.Okx.Configurations;
using Analyzer.Backend.Okx.Constants;
using Analyzer.Backend.Okx.Messages.Http;
using Microsoft.Extensions.Options;

namespace Analyzer.Backend.Okx.Services;

public class OkxHttpService(
    IOptions<OkxAuthConfiguration> okxAuthConfiguration,
    ILogger<OkxHttpService> logger,
    IHttpClientFactory httpClientFactory)
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        TypeInfoResolver = OkxJsonContext.Default,
    };

    public async Task<OkxBalanceDetail[]> GetBalanceAsync()
    {
        const string endpoint = "/api/v5/asset/balances";
        OkxBalanceDetail[]? response = await SendRequestAsync<OkxBalanceDetail>("GET", endpoint);
        return response ?? [];
    }

    public async Task<OkxFundingBalance[]> GetFundingBalanceAsync(string? currency = null)
    {
        const string endpoint = "/api/v5/asset/balances";
        var parameters = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(currency))
        {
            parameters["ccy"] = currency;
        }

        OkxFundingBalance[] response = await SendRequestAsync<OkxFundingBalance>("GET", endpoint, parameters) ?? [];
        return response;
    }

    public async Task<OkxOrder[]> GetPendingOrdersAsync(string? instType = null, string? instId = null)
    {
        const string endpoint = "/api/v5/trade/orders-pending";
        var parameters = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(instType))
        {
            parameters["instType"] = instType;
        }

        if (!string.IsNullOrEmpty(instId))
        {
            parameters["instId"] = instId;
        }

        OkxOrder[] response = await SendRequestAsync<OkxOrder>("GET", endpoint, parameters) ?? [];
        return response;
    }

    public async Task<OkxOrder[]> GetBuyOrdersAsync(string? instId = null)
    {
        OkxOrder[] orders = await GetPendingOrdersAsync(InstrumentType.Spot, instId);
        orders = orders.Where(x => x.Side == OkxOrderSide.Buy).ToArray();

        return orders;
    }

    public async Task<OkxOrder[]> GetSellOrdersAsync(string? instId = null)
    {
        OkxOrder[] orders = await GetPendingOrdersAsync(InstrumentType.Spot, instId);
        orders = orders.Where(x => x.Side == OkxOrderSide.Sell).ToArray();

        return orders;
    }

    public async Task<OkxOrder[]> GetOrderHistoryAsync(string? instType = null, string? instId = null, int limit = 100)
    {
        const string endpoint = "/api/v5/trade/orders-history";
        var parameters = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString()
        };

        if (!string.IsNullOrEmpty(instType))
        {
            parameters["instType"] = instType;
        }

        if (!string.IsNullOrEmpty(instId))
        {
            parameters["instId"] = instId;
        }

        OkxOrder[] response = await SendRequestAsync<OkxOrder>("GET", endpoint, parameters) ?? [];
        return response;
    }

    public async Task<OkxTicker[]> GetTickerAsync(string instId)
    {
        const string endpoint = "/api/v5/market/ticker";
        var parameters = new Dictionary<string, string>
        {
            ["instId"] = instId
        };

        OkxTicker[] response = await SendRequestAsync<OkxTicker>("GET", endpoint, parameters) ?? [];
        return response;
    }

    public async Task<OkxTicker[]> GetAllTickersAsync(string instType = "SPOT")
    {
        const string endpoint = "/api/v5/market/tickers";
        var parameters = new Dictionary<string, string>
        {
            ["instType"] = instType
        };

        OkxTicker[] response = await SendRequestAsync<OkxTicker>("GET", endpoint, parameters) ?? [];
        return response;
    }

    public async Task<OkxOrderBook[]> GetOrderBookAsync(string instId, int depth = 20)
    {
        const string endpoint = "/api/v5/market/books";
        var parameters = new Dictionary<string, string>
        {
            ["instId"] = instId,
            ["sz"] = depth.ToString()
        };

        OkxOrderBook[] response = await SendRequestAsync<OkxOrderBook>("GET", endpoint, parameters) ?? [];
        return response;
    }

    private async Task<T[]?> SendRequestAsync<T>(string method, string endpoint, Dictionary<string, string>? parameters = null)
    {
        using HttpClient httpClient = httpClientFactory.CreateClient("Okx");

        string requestPath = BuildRequestPath();
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string signature = OkxAuthService.GenerateSignature(timestamp, okxAuthConfiguration.Value.SecretKey, method, requestPath);

        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-SIGN", signature);
        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-TIMESTAMP", timestamp);

        try
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestPath);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string errorContent = await httpResponse.Content.ReadAsStringAsync();
                logger.LogError("HTTP request failed: {StatusCode} - {Content}", httpResponse.StatusCode, errorContent);
                return null;
            }

            string jsonString = await httpResponse.Content.ReadAsStringAsync();
            OkxHttpResponse<T>? response = JsonSerializer.Deserialize<OkxHttpResponse<T>>(jsonString, serializerOptions);
            return response!.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error making authenticated request to {Endpoint}", endpoint);
            return null;
        }

        string BuildRequestPath()
        {
            if (parameters == null || parameters.Count == 0)
            {
                return endpoint;
            }

            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            foreach (KeyValuePair<string, string> param in parameters)
            {
                query[param.Key] = param.Value;
            }

            return $"{endpoint}?{query}";
        }
    }
}

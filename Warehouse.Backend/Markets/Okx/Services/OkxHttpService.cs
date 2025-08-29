using System.Collections.Specialized;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using Warehouse.Backend.Core;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Markets.Okx.Messages.Http;

namespace Warehouse.Backend.Markets.Okx.Services;

public class OkxHttpService(
    IOptions<MarketCredentials> okxAuthConfiguration,
    ILogger<OkxHttpService> logger,
    IHttpClientFactory httpClientFactory)
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        TypeInfoResolver = OkxJsonContext.Default
    };

    public void Configure(MarketCredentials credentials)
    {
        // todo: remove DI injection of httpclientfactory and move creation here
    }

    public async Task<Result<OkxBalanceDetail[]>> GetBalanceAsync()
    {
        const string endpoint = "/api/v5/asset/balances";
        Result<OkxBalanceDetail[]> response = await SendRequestAsync<OkxBalanceDetail[]>("GET", endpoint);
        return response;
    }

    public async Task<Result<OkxFundingBalance[]>> GetFundingBalanceAsync(string? currency = null)
    {
        const string endpoint = "/api/v5/asset/balances";
        var parameters = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(currency))
        {
            parameters["ccy"] = currency;
        }

        Result<OkxFundingBalance[]> response = await SendRequestAsync<OkxFundingBalance[]>("GET", endpoint, parameters);
        return response;
    }

    public async Task<Result<OkxOrder[]>> GetPendingOrdersAsync(string? instType = null, string? instId = null)
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

        Result<OkxOrder[]> response = await SendRequestAsync<OkxOrder[]>("GET", endpoint, parameters);
        return response;
    }

    public async Task<Result<OkxOrder[]>> GetOrderHistoryAsync(string? instType = null, string? instId = null, int limit = 100)
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

        Result<OkxOrder[]> response = await SendRequestAsync<OkxOrder[]>("GET", endpoint, parameters);
        return response;
    }

    public async Task<Result<OkxTicker[]>> GetTickerAsync(string instId)
    {
        const string endpoint = "/api/v5/market/ticker";
        var parameters = new Dictionary<string, string>
        {
            ["instId"] = instId
        };

        Result<OkxTicker[]> response = await SendRequestAsync<OkxTicker[]>("GET", endpoint, parameters);
        return response;
    }

    public async Task<Result<OkxTicker[]>> GetAllTickersAsync(string instType = "SPOT")
    {
        const string endpoint = "/api/v5/market/tickers";
        var parameters = new Dictionary<string, string>
        {
            ["instType"] = instType
        };

        Result<OkxTicker[]> response = await SendRequestAsync<OkxTicker[]>("GET", endpoint, parameters);
        return response;
    }

    public async Task<Result<OkxOrderBook[]>> GetOrderBookAsync(string instId, int depth = 20)
    {
        const string endpoint = "/api/v5/market/books";
        var parameters = new Dictionary<string, string>
        {
            ["instId"] = instId,
            ["sz"] = depth.ToString()
        };

        Result<OkxOrderBook[]> response = await SendRequestAsync<OkxOrderBook[]>("GET", endpoint, parameters);
        return response;
    }

    private async Task<Result<T>> SendRequestAsync<T>(string method, string endpoint, Dictionary<string, string>? parameters = null)
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
                return Result<T>.Failure("HTTP request failed", httpResponse.StatusCode);
            }

            string jsonString = await httpResponse.Content.ReadAsStringAsync();
            OkxHttpResponse<T>? response = JsonSerializer.Deserialize<OkxHttpResponse<T>>(jsonString, serializerOptions);
            return Result<T>.Success(response!.Data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error making authenticated request to {Endpoint}", endpoint);
            return Result<T>.Failure(new Error($"Error making authenticated request to {endpoint}"));
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

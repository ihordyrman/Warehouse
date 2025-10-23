using System.Collections.Specialized;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Markets.Concrete.Okx.Constants;
using Warehouse.Core.Markets.Concrete.Okx.Messages.Http;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Markets.Concrete.Okx.Services;

public class OkxHttpService(ILogger<OkxHttpService> logger, IHttpClientFactory httpClientFactory, ICredentialsProvider credentialsProvider)
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        TypeInfoResolver = OkxJsonContext.Default
    };

    private MarketCredentials? Credentials { get; set; }

    public async Task<Result<OkxBalanceDetail[]>> GetBalanceAsync(string? currency = null)
    {
        const string endpoint = "/api/v5/asset/balances";
        var parameters = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(currency))
        {
            parameters["ccy"] = currency;
        }

        return await SendRequestAsync<OkxBalanceDetail[]>("GET", endpoint, parameters);
    }

    public async Task<Result<OkxFundingBalance[]>> GetFundingBalanceAsync(string? currency = null)
    {
        const string endpoint = "/api/v5/asset/balances";
        var parameters = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(currency))
        {
            parameters["ccy"] = currency;
        }

        return await SendRequestAsync<OkxFundingBalance[]>("GET", endpoint, parameters);
    }

    public async Task<Result<OkxAccountBalance[]>> GetAccountBalanceAsync(string? currency = null)
    {
        const string endpoint = "/api/v5/account/balance";
        var parameters = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(currency))
        {
            parameters["ccy"] = currency;
        }

        return await SendRequestAsync<OkxAccountBalance[]>("GET", endpoint, parameters);
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

        return await SendRequestAsync<OkxOrder[]>("GET", endpoint, parameters);
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

        return await SendRequestAsync<OkxOrder[]>("GET", endpoint, parameters);
    }

    public async Task<Result<OkxTicker[]>> GetTickerAsync(string instId)
    {
        const string endpoint = "/api/v5/market/ticker";
        var parameters = new Dictionary<string, string>
        {
            ["instId"] = instId
        };

        return await SendRequestAsync<OkxTicker[]>("GET", endpoint, parameters);
    }

    public async Task<Result<OkxTicker[]>> GetAllTickersAsync(string instType = InstrumentType.Spot)
    {
        const string endpoint = "/api/v5/market/tickers";
        var parameters = new Dictionary<string, string>
        {
            ["instType"] = instType
        };

        return await SendRequestAsync<OkxTicker[]>("GET", endpoint, parameters);
    }

    public async Task<Result<OkxOrderBook[]>> GetOrderBookAsync(string instId, int depth = 20)
    {
        const string endpoint = "/api/v5/market/books";
        var parameters = new Dictionary<string, string>
        {
            ["instId"] = instId,
            ["sz"] = depth.ToString()
        };

        return await SendRequestAsync<OkxOrderBook[]>("GET", endpoint, parameters);
    }

    public async Task<Result<OkxAssetsValuation[]>> GetAssetsValuationAsync(string valuationCurrency = "USDT")
    {
        const string endpoint = "/api/v5/asset/asset-valuation";
        var parameters = new Dictionary<string, string>
        {
            ["ccy"] = valuationCurrency
        };

        return await SendRequestAsync<OkxAssetsValuation[]>("GET", endpoint, parameters);
    }

    public async Task<Result<OkxCandlestick[]>> GetCandlesticksAsync(
        string instId,
        string? bar = null,
        string? after = null,
        string? before = null,
        int? limit = null)
    {
        const string endpoint = "/api/v5/market/candles";
        var parameters = new Dictionary<string, string>
        {
            ["instId"] = instId
        };

        if (!string.IsNullOrEmpty(bar))
        {
            parameters["bar"] = bar;
        }

        if (!string.IsNullOrEmpty(after))
        {
            parameters["after"] = after;
        }

        if (!string.IsNullOrEmpty(before))
        {
            parameters["before"] = before;
        }

        if (limit.HasValue)
        {
            parameters["limit"] = limit.Value.ToString();
        }

        return await SendRequestAsync<OkxCandlestick[]>("GET", endpoint, parameters);
    }

    private async Task<Result<T>> SendRequestAsync<T>(string method, string endpoint, Dictionary<string, string>? parameters = null)
    {
        if (Credentials is null)
        {
            Credentials = await credentialsProvider.GetCredentialsAsync(MarketType.Okx);
            if (Credentials is null)
            {
                return Result<T>.Failure(new Error("Credentials are not found"));
            }
        }

        using HttpClient httpClient = httpClientFactory.CreateClient("Okx");

        string requestPath = BuildRequestPath();
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string signature = OkxAuthService.GenerateSignature(timestamp, Credentials.SecretKey, method, requestPath);

        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-SIGN", signature);
        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-TIMESTAMP", timestamp);
        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", Credentials.ApiKey);
        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-PASSPHRASE", Credentials.Passphrase);

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
            if (parameters is null || parameters.Count == 0)
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

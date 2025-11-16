using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Warehouse.App.Pages;

public class BalanceModel(IHttpClientFactory httpClientFactory, IConfiguration configuration) : PageModel
{
    public decimal Available { get; set; }

    public decimal InOrders { get; set; }

    public decimal Total { get; set; }

    public async Task<IActionResult> OnGetAsync(string marketType)
    {
        HttpClient client = httpClientFactory.CreateClient();
        string baseUrl = configuration["ApiBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        try
        {
            // Get account balance summary
            HttpResponseMessage summaryResponse = await client.GetAsync($"{baseUrl}/balance/{marketType}/account/summary");
            if (summaryResponse.IsSuccessStatusCode)
            {
                string json = await summaryResponse.Content.ReadAsStringAsync();
                AccountBalanceResponse? summary = JsonSerializer.Deserialize<AccountBalanceResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (summary != null)
                {
                    Available = summary.TotalAvailable;
                    InOrders = summary.TotalInOrders;
                    Total = summary.TotalBalance;
                    return Page();
                }
            }

            // Fallback to total USDT value
            HttpResponseMessage totalResponse = await client.GetAsync($"{baseUrl}/balance/{marketType}/total-usdt");
            if (totalResponse.IsSuccessStatusCode)
            {
                string json = await totalResponse.Content.ReadAsStringAsync();
                TotalUsdtResponse? totalData = JsonSerializer.Deserialize<TotalUsdtResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                Total = totalData?.TotalUsdtValue ?? 0;
                Available = Total;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading balance for {marketType}: {ex.Message}");
        }

        return Page();
    }

    private class AccountBalanceResponse
    {
        public decimal TotalBalance { get; set; }

        public decimal TotalAvailable { get; set; }

        public decimal TotalInOrders { get; set; }
    }

    private class TotalUsdtResponse
    {
        public decimal TotalUsdtValue { get; set; }
    }
}

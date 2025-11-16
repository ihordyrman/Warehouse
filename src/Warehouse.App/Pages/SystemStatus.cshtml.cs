using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Warehouse.App.Pages;

public class SystemStatusModel(IHttpClientFactory httpClientFactory, IConfiguration configuration) : PageModel
{
    public string StatusText { get; set; } = "Idle";

    public string StatusClass { get; set; } = "badge";

    public async Task<IActionResult> OnGetAsync()
    {
        HttpClient client = httpClientFactory.CreateClient();
        string baseUrl = configuration["ApiBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        try
        {
            // Check if any workers are running
            HttpResponseMessage workersResponse = await client.GetAsync($"{baseUrl}/worker/enabled");
            if (workersResponse.IsSuccessStatusCode)
            {
                string json = await workersResponse.Content.ReadAsStringAsync();
                List<WorkerInfo>? workers = JsonSerializer.Deserialize<List<WorkerInfo>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (workers != null && workers.Any())
                {
                    StatusText = "Online";
                    StatusClass = "badge badge-success";
                    return Page();
                }
            }

            // Check if any accounts are configured
            HttpResponseMessage marketsResponse = await client.GetAsync($"{baseUrl}/market");
            if (marketsResponse.IsSuccessStatusCode)
            {
                string json = await marketsResponse.Content.ReadAsStringAsync();
                List<MarketInfo>? markets = JsonSerializer.Deserialize<List<MarketInfo>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (markets != null && markets.Any(m => m.Enabled))
                {
                    StatusText = "Idle";
                    StatusClass = "badge badge-warning";
                    return Page();
                }
            }

            StatusText = "Not Configured";
            StatusClass = "badge";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting system status: {ex.Message}");
            StatusText = "Error";
            StatusClass = "badge badge-danger";
        }

        return Page();
    }

    private class WorkerInfo
    {
        public int Id { get; set; }

        public bool Enabled { get; set; }
    }

    private class MarketInfo
    {
        public int Id { get; set; }

        public bool Enabled { get; set; }
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Warehouse.App.Pages;

public class WorkersModel(IHttpClientFactory httpClientFactory, IConfiguration configuration) : PageModel
{
    public List<WorkerInfo> Workers { get; set; } = [];

    public bool OnlyEnabled { get; set; }

    public async Task<IActionResult> OnGetAsync(bool onlyEnabled = false)
    {
        OnlyEnabled = onlyEnabled;
        HttpClient client = httpClientFactory.CreateClient();
        string baseUrl = configuration["ApiBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        try
        {
            string endpoint = onlyEnabled ? "/worker/enabled" : "/worker";
            HttpResponseMessage response = await client.GetAsync($"{baseUrl}{endpoint}");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                Workers = JsonSerializer.Deserialize<List<WorkerInfo>>(
                              json,
                              new JsonSerializerOptions
                              {
                                  PropertyNameCaseInsensitive = true
                              }) ??
                          new List<WorkerInfo>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading workers: {ex.Message}");
        }

        return Page();
    }

    public class WorkerInfo
    {
        public int Id { get; set; }

        public string Symbol { get; set; } = "";

        public string MarketType { get; set; } = "";

        public bool Enabled { get; set; }

        public string? Strategy { get; set; }

        public string? Interval { get; set; }

        public DateTime? LastRun { get; set; }

        public DateTime? LastExecutedAt { get; set; }
    }
}

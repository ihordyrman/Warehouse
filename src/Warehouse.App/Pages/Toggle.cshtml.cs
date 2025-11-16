using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Warehouse.App.Pages;

public class ToggleModel(IHttpClientFactory httpClientFactory, IConfiguration configuration) : PageModel
{
    public WorkerInfo? Worker { get; set; }

    public async Task<IActionResult> OnPostAsync(int id, bool enabled)
    {
        HttpClient client = httpClientFactory.CreateClient();
        string baseUrl = configuration["ApiBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        try
        {
            // Toggle the worker state
            var toggleRequest = new { enabled };
            string json = JsonSerializer.Serialize(toggleRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Note: Using the existing endpoint pattern from your API
            HttpResponseMessage toggleResponse = await client.PostAsync($"{baseUrl}/api/workers/{id}/toggle", content);

            if (!toggleResponse.IsSuccessStatusCode)
            {
                // If the toggle endpoint doesn't exist, try the PUT endpoint to update the worker
                HttpResponseMessage workerResponse = await client.GetAsync($"{baseUrl}/worker/{id}");
                if (workerResponse.IsSuccessStatusCode)
                {
                    string workerJson = await workerResponse.Content.ReadAsStringAsync();
                    WorkerInfo? worker = JsonSerializer.Deserialize<WorkerInfo>(
                        workerJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (worker != null)
                    {
                        worker.Enabled = enabled;
                        string updateJson = JsonSerializer.Serialize(worker);
                        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                        await client.PutAsync($"{baseUrl}/worker/{id}", updateContent);
                    }
                }
            }

            // Fetch the updated worker data
            HttpResponseMessage response = await client.GetAsync($"{baseUrl}/worker/{id}");
            if (response.IsSuccessStatusCode)
            {
                string workerJson = await response.Content.ReadAsStringAsync();
                Worker = JsonSerializer.Deserialize<WorkerInfo>(
                    workerJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling worker {id}: {ex.Message}");
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

        public string? StrategyName { get; set; }

        public string? Interval { get; set; }

        public DateTime? LastRun { get; set; }

        public DateTime? LastExecutedAt { get; set; }
    }
}

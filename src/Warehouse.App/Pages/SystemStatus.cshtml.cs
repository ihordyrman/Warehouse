using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;

namespace Warehouse.App.Pages;

public class SystemStatusModel(WarehouseDbContext db) : PageModel
{
    public string StatusText { get; set; } = "Idle";

    public string StatusClass { get; set; } = "badge";

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            bool hasEnabledWorkers = await db.WorkerDetails.AsNoTracking().AnyAsync(x => x.Enabled);

            if (hasEnabledWorkers)
            {
                StatusText = "Online";
                StatusClass = "badge badge-success";
                return Page();
            }

            bool hasEnabledMarkets = await db.MarketDetails.AsNoTracking().AnyAsync();

            if (hasEnabledMarkets)
            {
                StatusText = "Idle";
                StatusClass = "badge badge-warning";
                return Page();
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
}

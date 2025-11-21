using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;

namespace Warehouse.App.Pages;

public class SystemStatusModel(WarehouseDbContext db) : PageModel
{
    public string StatusText { get; set; } = "Idle";

    public string StatusClass { get; set; } = "bg-gray-100 text-gray-800";

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            bool hasEnabledWorkers = await db.WorkerDetails.AsNoTracking().AnyAsync(x => x.Enabled);

            if (hasEnabledWorkers)
            {
                StatusText = "Online";
                StatusClass = "bg-green-100 text-green-800";
                return Page();
            }

            bool hasEnabledMarkets = await db.MarketDetails.AsNoTracking().AnyAsync();

            if (hasEnabledMarkets)
            {
                StatusText = "Idle";
                StatusClass = "bg-yellow-100 text-yellow-800";
                return Page();
            }

            StatusText = "Not Configured";
            StatusClass = "bg-gray-100 text-gray-800";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting system status: {ex.Message}");
            StatusText = "Error";
            StatusClass = "bg-red-100 text-red-800";
        }

        return Page();
    }
}

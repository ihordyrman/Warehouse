using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Warehouse.App.Old.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class NotFoundModel(ILogger<NotFoundModel> logger) : PageModel
{
    public string? RequestedPath { get; set; }

    public void OnGet()
    {
        RequestedPath = HttpContext.Request.Path;
        logger.LogWarning("404 Not Found: {Path}", RequestedPath);
    }
}

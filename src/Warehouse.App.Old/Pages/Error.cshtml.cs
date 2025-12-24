using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Warehouse.App.Old.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel(ILogger<ErrorModel> logger, IWebHostEnvironment environment) : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public bool IsDevelopment => environment.IsDevelopment();

    public string? ExceptionMessage { get; set; }

    public string? StackTrace { get; set; }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (!IsDevelopment)
        {
            return;
        }

        IExceptionHandlerFeature? exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature == null)
        {
            return;
        }

        ExceptionMessage = exceptionFeature.Error.Message;
        StackTrace = exceptionFeature.Error.StackTrace;
        logger.LogError(exceptionFeature.Error, "An unhandled exception occurred");
    }
}

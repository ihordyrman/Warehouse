using Microsoft.AspNetCore.Mvc;

namespace Warehouse.App.Endpoints.Validation;

public class ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failed: {Message}", ex.Message);
            await HandleValidationExceptionAsync(context, ex);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var errors = ex.ValidationResults.GroupBy(x => x.MemberNames.FirstOrDefault() ?? "")
            .ToDictionary(
                x => string.IsNullOrEmpty(x.Key) ? "General" : x.Key,
                x => x.Select(y => y.ErrorMessage ?? "Invalid value").ToArray());

        var validationProblemDetails = new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Status = 400,
            Instance = context.Request.Path
        };

        validationProblemDetails.Extensions.Add("traceId", context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(validationProblemDetails);
    }
}

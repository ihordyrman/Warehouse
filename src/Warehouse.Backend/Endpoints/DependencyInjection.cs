using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Endpoints;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddOpenApi();
        services.AddRateLimiter(options =>
        {
            options.AddTokenBucketLimiter(
                "ApiPolicy",
                config =>
                {
                    config.TokenLimit = 50;
                    config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    config.QueueLimit = 50;
                    config.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                    config.TokensPerPeriod = 50;
                    config.AutoReplenishment = true;
                });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    await context.HttpContext.Response.WriteAsync(
                        $"Too many requests. Please try again after {retryAfter.TotalMinutes} minute(s).",
                        token);
                }
                else
                {
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                }
            };
        });

        services.AddHttpLogging(x =>
        {
            if (env.IsDevelopment())
            {
                x.CombineLogs = true;
                x.LoggingFields = HttpLoggingFields.ResponseBody | HttpLoggingFields.ResponseHeaders;
            }
        });

        return services;
    }

    public static IApplicationBuilder AddApi(this WebApplication app)
    {
        app.UseStatusCodePages();
        app.UseHttpLogging();
        app.UseRateLimiter();
        app.MapScalarApiReference();
        app.MapOpenApi();
        app.UseMiddleware<ValidationExceptionMiddleware>();
        app.MapMarketEndpoints();
        app.MapMarketCredentialsEndpoints();
        app.MapWorkerEndpoints();
        app.MapPipelineStepEndpoints();
        app.MapBalanceEndpoints();

        return app;
    }
}

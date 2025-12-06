using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Core;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Concrete.Okx;
using Warehouse.Core.Markets.Domain;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoreDependencies(builder.Configuration);
builder.Services.AddOkxSupport(builder.Configuration);
builder.Services.AddRazorPages();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHttpLogging(x =>
{
    if (builder.Environment.IsDevelopment())
    {
        x.CombineLogs = true;
        x.LoggingFields = HttpLoggingFields.ResponseBody | HttpLoggingFields.ResponseHeaders;
    }
});

WebApplication app = builder.Build();

app.UseCors();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        IExceptionHandlerFeature? exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        Exception? exception = exceptionHandlerFeature?.Error;

        ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "An unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Detail = "An unexpected error occurred. Please try again later."
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

await EnsureDbReadinessAsync(app);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/NotFound");
app.UseHttpLogging();
app.MapRazorPages().WithStaticAssets();
app.Run();

static async Task EnsureDbReadinessAsync(WebApplication app)
{
    IServiceScopeFactory? scopeFactory = app.Services.GetService<IServiceScopeFactory>();
    using IServiceScope scope = scopeFactory!.CreateScope();
    WarehouseDbContext? dbContext = scope.ServiceProvider.GetService<WarehouseDbContext>();

    IConfiguration configuration = scope.ServiceProvider.GetService<IConfiguration>()!;

    await dbContext!.Database.EnsureCreatedAsync();
    await EnsureCredentialsPopulated(configuration, dbContext);
}

static async Task EnsureCredentialsPopulated(IConfiguration configuration, WarehouseDbContext dbContext)
{
    if (await dbContext.MarketCredentials.AnyAsync())
    {
        return;
    }

    const string section = "OkxAuthConfiguration";
    string apiKey = configuration[$"{section}:ApiKey"] ?? throw new ArgumentNullException();
    string passPhrase = configuration[$"{section}:Passphrase"] ?? throw new ArgumentNullException();
    string secretKey = configuration[$"{section}:SecretKey"] ?? throw new ArgumentNullException();

    Market? market = await dbContext.Markets.FirstOrDefaultAsync(x => x.Type == MarketType.Okx);
    if (market is null)
    {
        market = new Market
        {
            Type = MarketType.Okx
        };

        dbContext.Markets.Add(market);
    }

    var marketCredentials = new MarketCredentials
    {
        ApiKey = apiKey,
        Passphrase = passPhrase,
        SecretKey = secretKey,
        Market = market,
        IsSandbox = true
    };

    dbContext.MarketCredentials.Add(marketCredentials);
    await dbContext.SaveChangesAsync();
}

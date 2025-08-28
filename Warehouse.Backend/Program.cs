using Warehouse.Backend.Core;
using Warehouse.Backend.Features.Endpoints;
using Warehouse.Backend.Features.Okx;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi(builder.Environment);
builder.Services.AddCoreDependencies();
builder.Services.AddOkxSupport(builder.Configuration);

WebApplication app = builder.Build();
await app.EnsureDbReadinessAsync();
app.AddApi();
app.Run();

using Warehouse.Backend.Core.Extensions;
using Warehouse.Backend.Okx.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCoreDependencies();
builder.Services.AddOkxSupport(builder.Configuration);

IHost host = builder.Build();

host.Run();

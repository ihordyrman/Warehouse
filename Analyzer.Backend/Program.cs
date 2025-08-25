using Analyzer.Backend.Core;
using Analyzer.Backend.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOkxSupport(builder.Configuration);
builder.Services.AddScoped<WebSocketClient>();

IHost host = builder.Build();
host.Run();

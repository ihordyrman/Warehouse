using Analyzer.Backend;
using Analyzer.Backend.Okx;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OkxConfiguration>(builder.Configuration.GetSection(nameof(OkxConfiguration)));
builder.Services.AddScoped<OkxWsClient>();
builder.Services.AddHostedService<OkxMarketWorker>();

IHost host = builder.Build();
host.Run();

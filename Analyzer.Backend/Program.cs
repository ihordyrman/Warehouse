using System.Threading.Channels;
using Analyzer.Backend;
using Analyzer.Backend.Okx;
using Analyzer.Backend.Okx.Configurations;
using Analyzer.Backend.Okx.Processors;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OkxAuthConfiguration>(builder.Configuration.GetSection(nameof(OkxAuthConfiguration)));
builder.Services.AddKeyedSingleton(
    OkxChannelNames.RawMessages,
    (_, _) =>
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = true
        };
        return Channel.CreateBounded<string>(options);
    });

builder.Services.AddScoped<OkxWsClient>();
builder.Services.AddSingleton<OkxRawMessageProcessor>();
builder.Services.AddHostedService<OkxMarketWorker>();

IHost host = builder.Build();
host.Run();

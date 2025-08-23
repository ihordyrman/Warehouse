using Analyzer.Backend.Okx;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<OkxConfiguration>(builder.Configuration.GetSection(nameof(OkxConfiguration)));
builder.Services.AddSingleton<OkxClient>();
WebApplication app = builder.Build();

OkxClient client = app.Services.GetService<OkxClient>()!;
await client.ConnectAsync(ChannelType.Private);
app.Run();

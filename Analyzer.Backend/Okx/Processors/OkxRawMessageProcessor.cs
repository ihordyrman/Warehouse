using System.Text.Json;
using System.Threading.Channels;
using Analyzer.Backend.Okx.Messages;
using Analyzer.Backend.Okx.Models;

namespace Analyzer.Backend.Okx.Processors;

public class OkxRawMessageProcessor(
    [FromKeyedServices(OkxChannelNames.RawMessages)] Channel<string> messageChannel,
    [FromKeyedServices(OkxChannelNames.MarketData)] Channel<MarketData> marketDataChannel,
    ILogger<OkxRawMessageProcessor> logger)
{
    private readonly ChannelReader<string> messageReader = messageChannel.Reader;
    private readonly ChannelWriter<MarketData> marketDataWriter = marketDataChannel.Writer;
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        TypeInfoResolver = OkxJsonContext.Default
    };

    public async Task StartProcessingAsync(CancellationToken cancellationToken)
    {
        await foreach (string rawMessage in messageReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                OkxSocketSubscriptionResponse? response =
                    JsonSerializer.Deserialize<OkxSocketSubscriptionResponse>(rawMessage, serializerOptions);

                if (response!.Event == OkxEvent.Subscribe)
                {
                    logger.LogInformation("Successfully subscribed to {Channel}:{Instrument}", response.Arguments!.Channel, response.Arguments.InstrumentId);
                    continue;
                }

                if (response?.Data?.Length > 0)
                {
                    var marketData = new MarketData(
                        response.Arguments!.Channel!,
                        response.Arguments.InstrumentId!,
                        response.Data[0].Asks!,
                        response.Data[0].Bids!);
                    await marketDataWriter.WriteAsync(marketData, cancellationToken);
                    continue;
                }

                logger.LogWarning("Unable to parse raw message: {Message}", rawMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");
            }
        }
    }
}

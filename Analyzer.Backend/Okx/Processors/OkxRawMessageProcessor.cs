using System.Text;
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
    private readonly StringBuilder stringBuilder = new();
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
                OkxSocketSubscriptionResponse? response = null!;
                if (IsCompleteJson(rawMessage))
                {
                    response = JsonSerializer.Deserialize<OkxSocketSubscriptionResponse>(rawMessage, serializerOptions);
                }
                else
                {
                    stringBuilder.Append(rawMessage);
                    if (stringBuilder.Length != rawMessage.Length && IsCompleteJson(stringBuilder.ToString()))
                    {
                        string raw = stringBuilder.ToString();
                        stringBuilder.Clear();
                        response = JsonSerializer.Deserialize<OkxSocketSubscriptionResponse>(raw, serializerOptions);
                    }
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

                logger.LogWarning("Unable to parse raw message");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");
            }
        }
    }

    public static bool IsCompleteJson(string json)
    {
        int braceCount = 0;
        int bracketCount = 0;
        bool inString = false;
        bool correctStructure = json.Length > 2 && json[0] is '{' && json[^1] is '}';

        if (!correctStructure)
        {
            return false;
        }

        foreach (char ch in json)
        {
            if (ch == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                _ = ch switch
                {
                    '{' => braceCount++,
                    '}' => braceCount--,
                    '[' => bracketCount++,
                    ']' => bracketCount--,
                    _ => -1
                };
            }
        }

        return braceCount == 0 && bracketCount == 0 && !inString;
    }
}

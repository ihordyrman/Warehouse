using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Warehouse.Core;
using Warehouse.Core.Application.Services;
using Warehouse.Core.Domain;
using static Warehouse.Core.Domain.Instrument;
using static Warehouse.Core.Domain.MarketType;

namespace Warehouse.Tools.Importer;

public static class Program
{
    public static async Task<int> Main()
    {
        AnsiConsole.Write(new FigletText("Warehouse Tools").Centered().Color(Color.Blue));
        AnsiConsole.WriteLine();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddCoreDependencies();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        try
        {
            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title("[green]Select a tool:[/]")
                    .AddChoices("Binance API", "Import CSV File", "Import Directory", "Exit"));

            switch (choice)
            {
                case "Binance API":
                    await ImportFromBinanceAsync(serviceProvider);
                    break;

                case "Import CSV File":
                    await ImportSingleFileAsync(serviceProvider);
                    break;

                case "Import Directory":
                    await ImportDirectoryAsync(serviceProvider);
                    break;

                case "Exit":
                    AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                    break;
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static async Task ImportFromBinanceAsync(ServiceProvider serviceProvider)
    {
        string instrument = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("[green]Select an instrument:[/]")
                .AddChoices(nameof(BTC), nameof(SOL), nameof(ETH), nameof(DOGE), nameof(XRP), nameof(BCH), nameof(LTC)));

        string limit = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("[green]Select an limit range:[/]").AddChoices("10", "100", "1000"));

        var httpClient = new HttpClient();
        int totalImported = 0;
        var start = new DateTime(2025, 01, 01);
        DateTime currentDate = start;
        ICandlestickService? candlestickService = serviceProvider.GetService<ICandlestickService>();

        while (currentDate < DateTime.Now - TimeSpan.FromMinutes(1))
        {
            long startTime = new DateTimeOffset(currentDate.ToUniversalTime()).ToUnixTimeMilliseconds();
            HttpResponseMessage response = await httpClient.GetAsync(
                $"https://api.binance.com/api/v3/klines?symbol={instrument}USDT&limit={limit}&interval=1m&startTime={startTime}");

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.WriteLine($"Failed to fetch data at {currentDate}. Status: {response.StatusCode}");
                return;
            }

            string json = await response.Content.ReadAsStringAsync();
            JsonElement candles = JsonSerializer.Deserialize<JsonElement>(json);

            var candlesticks = new List<Candlestick>();

            foreach (JsonElement candle in candles.EnumerateArray())
            {
                JsonElement[] array = candle.EnumerateArray().ToArray();

                long openTime = array[0].GetInt64();
                long closeTime = array[6].GetInt64();
                if (!Enum.TryParse(instrument, out Instrument inst))
                {
                    AnsiConsole.WriteLine("Failed to parse enum");
                    return;
                }

                var candlestick = new Candlestick
                {
                    Symbol = new Pair(inst, USDT),
                    MarketType = Binance,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(openTime).UtcDateTime,
                    Open = decimal.Parse(array[1].GetString()!),
                    High = decimal.Parse(array[2].GetString()!),
                    Low = decimal.Parse(array[3].GetString()!),
                    Close = decimal.Parse(array[4].GetString()!),
                    Volume = decimal.Parse(array[5].GetString()!),
                    VolumeQuote = decimal.Parse(array[7].GetString()!),
                    IsCompleted = closeTime < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Timeframe = "1m"
                };

                candlesticks.Add(candlestick);
            }

            int updated = await candlestickService!.SaveCandlesticksAsync(candlesticks);
            currentDate = candlesticks.Last().Timestamp;
            totalImported += updated;
            AnsiConsole.WriteLine($"Imported {updated} candles. Total: {totalImported}. Last timestamp: {candlesticks.Last().Timestamp}");
            await Task.Delay(100);
        }
    }

    private static async Task ImportDirectoryAsync(ServiceProvider serviceProvider)
    {
        string directory = AnsiConsole.Ask<string>("Enter directory path:");
        if (!Directory.Exists(directory))
        {
            AnsiConsole.MarkupLine("[red]Directory not found![/]");
            return;
        }

        string[] csvFiles = Directory.GetFiles(directory, "*.csv");
        if (csvFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]{directory}: Directory doesn't have csv files![/]");
        }

        foreach (string file in csvFiles)
        {
            await ImportFileAsync(serviceProvider, file);
        }
    }

    private static async Task ImportSingleFileAsync(ServiceProvider serviceProvider)
    {
        string filePath = AnsiConsole.Ask<string>("Enter CSV file path:");

        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[red]{filePath}: File not found![/]");
            return;
        }

        await ImportFileAsync(serviceProvider, filePath);
    }

    private static async Task ImportFileAsync(ServiceProvider serviceProvider, string fileName)
    {
        // considering we have format /home/ubuntu/okx-data/2025-10/OKB_candlesticks.cs
        string symbol = fileName.Split(".")[^2].Split("/")[^1].Split("_")[0] + "-USDT";

        AnsiConsole.WriteLine();

        List<CandlestickCsvRecord> candlesticks = [];
        ICandlestickService? candlestickService = serviceProvider.GetService<ICandlestickService>();
        await foreach (CandlestickCsvRecord record in ReadCsvFileAsync(fileName))
        {
            candlesticks.Add(record);
        }

        await candlestickService!.SaveCandlesticksAsync(
            candlesticks.Select(x => new Candlestick
            {
                Symbol = symbol, Timeframe = "1m",
                Close = x.Close,
                High = x.High,
                IsCompleted = true,
                Low = x.Low,
                MarketType = Okx,
                Open = x.Open,
                Volume = x.Volume,
                VolumeQuote = x.VolumeQuote,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(x.Timestamp).DateTime.ToUniversalTime()
            }));

        AnsiConsole.MarkupLine(
            candlesticks.Count > 0 ? $"[green]{fileName}: Import completed successfully![/]" : $"[red]{fileName}: Import failed![/]");
    }

    private static async IAsyncEnumerable<CandlestickCsvRecord> ReadCsvFileAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = context => AnsiConsole.WriteException(
                    new InvalidDataException($"Bad data found at row {context.Context.Parser?.Row}: {context.RawRecord}"))
            });

        await foreach (CandlestickCsvRecord record in csv.GetRecordsAsync<CandlestickCsvRecord>())
        {
            yield return record;
        }
    }
}

public class CandlestickCsvRecord
{
    [Index(0)]
    public long Timestamp { get; set; }

    [Index(1)]
    public decimal Open { get; set; }

    [Index(2)]
    public decimal High { get; set; }

    [Index(3)]
    public decimal Low { get; set; }

    [Index(4)]
    public decimal Close { get; set; }

    [Index(5)]
    public decimal Volume { get; set; }

    [Index(6)]
    public decimal VolumeQuote { get; set; }

    [Index(7)]
    public int Confirmed { get; set; }
}

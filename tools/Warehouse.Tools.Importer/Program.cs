using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Warehouse.Core;
using Warehouse.Core.Application.Services;
using Warehouse.Core.Domain;
using Warehouse.Core.Infrastructure;

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
                new SelectionPrompt<string>().Title("[green]Select a tool:[/]").AddChoices("Import CSV File", "Import Directory", "Exit"));

            switch (choice)
            {
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
                MarketType = MarketType.Okx,
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

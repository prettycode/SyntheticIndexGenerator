using Data.Extensions;
using Data.Returns;
using Data.SyntheticIndices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        using var serviceProvider = BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        await PrimeReturnsCache(serviceProvider, logger);
    }

    static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return new ServiceCollection()
            .AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole())
            .AddDataLibraryConfiguration(configuration)
            .BuildServiceProvider();
    }

    static async Task PrimeReturnsCache(IServiceProvider provider, ILogger<Program> logger)
    {
        var returnsService = provider.GetRequiredService<IReturnsService>();
        var indicesService = provider.GetRequiredService<ISyntheticIndicesService>();

        var periodTypes = Enum.GetValues<PeriodType>().Reverse();
        var syntheticIndexTickers = indicesService.GetSyntheticIndexTickers();
        var tickers = syntheticIndexTickers.Concat([
            "BLOK",
            "FDIG",
            "BITQ",
            "DAPP",
            "BKCH",
            "WGMI",
            "GLD",
            "SGOL",
            "GLDM",
            "DBMF",
            "KMLM",
            "CTA",
            "USFR"
        ]);

        // Concurrently

        var returnsHistoryTasks = new List<Task>();

        foreach (var ticker in tickers)
        {
            foreach (var periodType in periodTypes)
            {
                returnsHistoryTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await returnsService.GetReturnsHistory(
                            ticker,
                            periodType,
                            DateTime.MinValue,
                            DateTime.MaxValue);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "{ticker}: {periodType}: {exMessage}", ticker, periodType, ex.Message);

                        return null;
                    }
                }));
            }
        }

        await Task.WhenAll(returnsHistoryTasks);

        // Pause

        Console.WriteLine("\n\nPress Enter key to continue...\n");
        Console.ReadLine();

        // Serially

        var failures = new Dictionary<string, List<(PeriodType, string)>>();

        foreach (var ticker in indicesService.GetSyntheticIndexTickers())
        {
            foreach (var periodType in Enum.GetValues<PeriodType>().Reverse())
            {
                try
                {
                    await returnsService.GetReturnsHistory(
                        ticker,
                        periodType,
                        DateTime.MinValue,
                        DateTime.MaxValue);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{ticker}: {periodType}: {exMessage}", ticker, periodType, ex.Message);

                    if (!failures.TryGetValue(ticker, out List<(PeriodType, string)>? value))
                    {
                        value = ([]);
                        failures[ticker] = value;
                    }

                    value.Add((periodType, ex.Message));
                }
            }
        }

        if (failures.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Failures:");

        foreach (var pair in failures)
        {
            foreach (var (periodType, message) in pair.Value)
            {
                Console.WriteLine($"{pair.Key}: {periodType}: {(message.Length > 100 ? message.Substring(0, 100) : message)}");
            }
        }
    }
}
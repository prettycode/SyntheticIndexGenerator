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
            "WGMI"
        ]);

        var returnsHistoryTasks = new List<Task>();

        foreach (var ticker in tickers)
        {
            foreach (var periodType in periodTypes)
            {
                Console.WriteLine();
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
                        Console.Error.WriteLine($"{ticker}: {periodType}: {ex.Message}");
                        return null; // or some other value to indicate failure
                    }
                }));
            }
        }

        await Task.WhenAll(returnsHistoryTasks);

        /*
        foreach (var ticker in indicesService.GetSyntheticIndexTickers())
        {
            foreach (var periodType in Enum.GetValues<PeriodType>().Reverse())
            {
                await returnsService.GetReturnsHistory(
                    ticker,
                    periodType,
                    DateTime.MinValue,
                    DateTime.MaxValue);
            }
        }*/
    }
}
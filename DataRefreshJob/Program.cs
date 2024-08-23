using System.Diagnostics;
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
        var returnsHistoryTasks = new List<Task>();

        logger.LogInformation("CLIENT: CONCURRENT: Requesting returns for synthetic indices…");

        foreach (var ticker in indicesService.GetSyntheticIndexTickers()/* TODO test case because they both share same underlying backfillticker: new[] { "$^ITSM", "$^ILCB" }*/)
        {
            foreach (var periodType in Enum.GetValues<PeriodType>().Reverse())
            {
                Console.WriteLine("\n<CLIENT>");

                logger.LogInformation(
                    "{ticker}: Requesting entire return history for period {periodType}…",
                    ticker,
                    periodType);

                returnsHistoryTasks.Add(returnsService.GetReturnsHistory(
                    ticker,
                    periodType,
                    DateTime.MinValue,
                    DateTime.MaxValue));

                Console.WriteLine("</CLIENT>\n");
            }
        }

        await Task.WhenAll(returnsHistoryTasks);

        logger.LogInformation("CLIENT: SERIAL: Requesting returns for synthetic indices…");

        foreach (var ticker in indicesService.GetSyntheticIndexTickers()/* TODO test case because they both share same underlying backfillticker: new[] { "$^ITSM", "$^ILCB" }*/)
        {
            foreach (var periodType in Enum.GetValues<PeriodType>().Reverse())
            {
                Console.WriteLine("\n<CLIENT>");

                logger.LogInformation(
                    "{ticker}: Requesting entire return history for period {periodType}…",
                    ticker,
                    periodType);

                await returnsService.GetReturnsHistory(
                    ticker,
                    periodType,
                    DateTime.MinValue,
                    DateTime.MaxValue);

                Console.WriteLine("</CLIENT>\n");
            }
        }
    }
}
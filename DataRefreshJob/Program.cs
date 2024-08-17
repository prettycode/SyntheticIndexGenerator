using System.Diagnostics;
using Data.Extensions;
using Data.Returns;
using Data.SyntheticIndex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        using var serviceProvider = BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        while (true)
        {
            if (!Debugger.IsAttached)
            {
                await WaitUntilNextMarketDataUpdate(logger);
            }

            try
            {
                await UpdateReturnsRepository(serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during data refresh.");
            }

            if (Debugger.IsAttached)
            {
                break;
            }
        }
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

    static async Task UpdateReturnsRepository(IServiceProvider provider)
    {
        /*static async Task UpdateReturnsRepositoryWithIndexBackfillTickers(
            ISyntheticIndexService syntheticIndexService,
            IReturnsService returnsService)
        {

            var allIndexBackfillTickers = syntheticIndexService.GetAllIndexBackfillTickers(true);

            await returnsService.GetReturnsHistory(allIndexBackfillTickers, PeriodType.Daily, DateTime.MinValue, DateTime.MaxValue);
            await returnsService.GetReturnsHistory(allIndexBackfillTickers, PeriodType.Monthly, DateTime.MinValue, DateTime.MaxValue);
            await returnsService.GetReturnsHistory(allIndexBackfillTickers, PeriodType.Yearly, DateTime.MinValue, DateTime.MaxValue);

            await Task.WhenAll(
                returnsService.GetReturnsHistory(allIndexBackfillTickers, PeriodType.Daily, DateTime.MinValue, DateTime.MaxValue),
                returnsService.GetReturnsHistory(allIndexBackfillTickers, PeriodType.Monthly, DateTime.MinValue, DateTime.MaxValue),
                returnsService.GetReturnsHistory(allIndexBackfillTickers, PeriodType.Yearly, DateTime.MinValue, DateTime.MaxValue));
        }

        static async Task UpdateReturnsRepositoryWithSyntheticIndexTickers(
            ISyntheticIndexService syntheticIndexService,
            IReturnsService returnsService)
        {
            var syntheticIndexTickers = returnsService.GetSyntheticIndexTickers();

            await returnsService.GetReturnsHistory(syntheticIndexTickers, PeriodType.Daily, DateTime.MinValue, DateTime.MaxValue);
            await returnsService.GetReturnsHistory(syntheticIndexTickers, PeriodType.Monthly, DateTime.MinValue, DateTime.MaxValue);
            await returnsService.GetReturnsHistory(syntheticIndexTickers, PeriodType.Yearly, DateTime.MinValue, DateTime.MaxValue);

            await Task.WhenAll(
                returnsService.GetReturnsHistory(syntheticIndexTickers, PeriodType.Daily, DateTime.MinValue, DateTime.MaxValue),
                returnsService.GetReturnsHistory(syntheticIndexTickers, PeriodType.Monthly, DateTime.MinValue, DateTime.MaxValue),
                returnsService.GetReturnsHistory(syntheticIndexTickers, PeriodType.Yearly, DateTime.MinValue, DateTime.MaxValue));
        }

        var returnsService = provider.GetRequiredService<IReturnsService>();
        var syntheticIndexService = provider.GetRequiredService<ISyntheticIndexService>();

        await UpdateReturnsRepositoryWithIndexBackfillTickers(syntheticIndexService, returnsService);
        await UpdateReturnsRepositoryWithSyntheticIndexTickers(syntheticIndexService, returnsService);
        */

        /*static async Task UpdateReturnsRepositoryWithIndexBackfillTickers(
            ISyntheticIndexService indicesService,
            IReturnsService returnsService)
        {
            var tickersNeeded = indicesService.GetIndexBackfillTickers();
            var returnsByTicker = await returnsService.GetReturns(tickersNeeded, false);
        }

        static async Task UpdateReturnsRepositoryWithSyntheticIndexTickers(
            ISyntheticIndexService indicesService,
            IReturnsService returnsService)
        {
            await indicesService.PutSyntheticIndicesInReturnsRepository();
        }

        var returnsService = provider.GetRequiredService<IReturnsService>();
        var indicesService = provider.GetRequiredService<ISyntheticIndexService>();

        await UpdateReturnsRepositoryWithIndexBackfillTickers(indicesService, returnsService);
        await UpdateReturnsRepositoryWithSyntheticIndexTickers(indicesService, returnsService);\
        */

        var returnsService = provider.GetRequiredService<IReturnsService>();
        var indicesService = provider.GetRequiredService<ISyntheticIndexService>();

        foreach (var ticker in indicesService.GetAllIndexBackfillTickers(true))
        {
            Console.WriteLine($"\nCLIENT: {ticker}: Fetching {PeriodType.Daily.ToString().ToUpper()} returns history…");
            await returnsService.GetReturnsHistory(ticker, PeriodType.Daily, DateTime.MinValue, DateTime.MaxValue);

            Console.WriteLine($"\nCLIENT: {ticker}: Fetching {PeriodType.Monthly.ToString().ToUpper()} returns history…");
            await returnsService.GetReturnsHistory(ticker, PeriodType.Monthly, DateTime.MinValue, DateTime.MaxValue);

            Console.WriteLine($"\nCLIENT: {ticker}: Fetching {PeriodType.Yearly.ToString().ToUpper()} returns history…");
            await returnsService.GetReturnsHistory(ticker, PeriodType.Yearly, DateTime.MinValue, DateTime.MaxValue);
        }
    }

    // TODO test
    static async Task WaitUntilNextMarketDataUpdate(ILogger<Program> logger)
    {
        var newYorkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var currentTimeNewYork = TimeZoneInfo.ConvertTime(DateTime.UtcNow, newYorkTimeZone);
        var nextMarketClosedTime = currentTimeNewYork.Date.AddHours(21);

        if (currentTimeNewYork >= nextMarketClosedTime)
        {
            nextMarketClosedTime = nextMarketClosedTime.AddDays(1);
        }

        nextMarketClosedTime = nextMarketClosedTime.DayOfWeek switch
        {
            DayOfWeek.Saturday => nextMarketClosedTime.AddDays(2),
            DayOfWeek.Sunday => nextMarketClosedTime.AddDays(1),
            _ => nextMarketClosedTime
        };

        var timeToWait = nextMarketClosedTime - currentTimeNewYork;
        var futureDateTime = currentTimeNewYork.Add(timeToWait);

        logger.LogInformation("Waiting until {futureDateTimeDay} {timeZone}",
            $"{futureDateTime:F}",
            newYorkTimeZone.DisplayName);

        await Task.Delay(timeToWait);
    }
}
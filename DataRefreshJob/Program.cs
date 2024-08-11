using System.Diagnostics;
using Data.Extensions;
using Data.Models;
using Data.Services;
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
        static async Task UpdateReturnsCacheWithIndexBackTestTickers(
            IQuotesService quotesService,
            IReturnsService returnsService)
        {
            var tickersNeeded = SyntheticIndex.GetBackFillTickers();
            var dailyPricesByTicker = await quotesService.GetPrices(tickersNeeded, true);
            var returnsByTicker = await returnsService.GetReturns(dailyPricesByTicker);
        }

        static async Task UpdateReturnsCacheWithSyntheticTickers(
            IQuotesService quotesService,
            IReturnsService returnsService)
        {
            await returnsService.PutSyntheticReturnsInReturnsRepository();

            var syntheticIndexTickers = SyntheticIndex.GetSyntheticIndexTickers();
            var syntheticConstituentDailyPricesByTicker = await quotesService.GetSyntheticIndexReturns(syntheticIndexTickers, true);
            var returnsByTicker = await returnsService.GetSyntheticIndexReturns(
                syntheticIndexTickers,
                syntheticConstituentDailyPricesByTicker);
        }

        var quotesService = provider.GetRequiredService<IQuotesService>();
        var returnsService = provider.GetRequiredService<IReturnsService>();

        await UpdateReturnsCacheWithIndexBackTestTickers(quotesService, returnsService);
        await UpdateReturnsCacheWithSyntheticTickers(quotesService, returnsService);
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
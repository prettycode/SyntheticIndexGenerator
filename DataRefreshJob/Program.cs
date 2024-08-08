﻿using System.Diagnostics;
using Data.Controllers;
using Data.Extensions;
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
                await RefreshData(serviceProvider, logger);
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

    static async Task RefreshData(IServiceProvider provider, ILogger<Program> logger)
    {
        var quotesManager = provider.GetRequiredService<QuotesService>();
        var returnsManager = provider.GetRequiredService<ReturnsService>();
        var indicesManager = provider.GetRequiredService<IndicesService>();
        var quoteTickersNeeded = IndicesService.GetBackfillTickers();

        var quotes = await quotesManager.GetQuotes(quoteTickersNeeded);

        if (quotes.Values.All(v => v == null))
        {
            logger.LogError("{message}", "Failed to refresh all data. Aborting downstream refreshes.");
            return;
        }

        await returnsManager.RefreshSyntheticReturns();
        await returnsManager.GetReturns(quoteTickersNeeded);
        await indicesManager.RefreshIndices();
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
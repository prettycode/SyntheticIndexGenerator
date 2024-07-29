﻿using FundHistoryCache.Controllers;
using FundHistoryCache.Models;
using FundHistoryCache.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timer = FundHistoryCache.Utils.Timer;

var settings = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.debug.json")
    .Build()
    .Get<AppSettings>() ?? throw new ApplicationException("Settings malformed.");

var quoteCache = new QuoteRepository(settings.QuoteRepositoryDataPath);
var returnCache = new ReturnRepository(settings.ReturnRepositoryDataPath, settings.SyntheticReturnsFilePath);

var quotesManager = new QuotesManager(quoteCache, CreateLogger<QuotesManager>());
var returnsManager = new ReturnsManager(quoteCache, returnCache, CreateLogger<ReturnsManager>());
var indicesManager = new IndicesManager(returnCache, CreateLogger<IndicesManager>());

var quoteTickersNeeded = IndicesManager.GetBackfillTickers();

await Timer.Exec("Refresh quotes", quotesManager.RefreshQuotes(quoteTickersNeeded));
await Timer.Exec("Refresh returns", returnsManager.RefreshReturns());
await Timer.Exec("Refresh indices", indicesManager.RefreshIndices());

GetPerformance(returnCache, quoteCache, "AVUV")
    .Result!
    .ForEach(tick => Console.WriteLine($"AVUV: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));

static ILogger<T> CreateLogger<T>() where T : class => LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<T>();

static async Task<List<PerformanceTick>?> GetPerformance(
    ReturnRepository returnsCache,
    QuoteRepository quoteCache,
    string ticker,
    decimal startingBalance = 100,
    ReturnPeriod granularity = ReturnPeriod.Daily)
{
    var tickerReturns = await returnsCache.Get(ticker, granularity);

    if (tickerReturns == null)
    {
        return null;
    }

    var performanceTicks = new List<PerformanceTick>();

    foreach (var currentReturnTick in tickerReturns)
    {
        performanceTicks.Add(new(currentReturnTick, startingBalance));
        startingBalance = performanceTicks[^1].EndingBalance;
    }

    return performanceTicks;
}
class AppSettings
{
    public string QuoteRepositoryDataPath { get; set; }
    public string ReturnRepositoryDataPath { get; set; }
    public string SyntheticReturnsFilePath { get; set; }
}

class PerformanceTick(PeriodReturn period, decimal startingBalance)
{
    public PeriodReturn Period { get; set; } = period;

    public decimal StartingBalance { get; set; } = startingBalance;

    public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

    public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }
}

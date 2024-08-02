using Data.Controllers;
using Data.Models;
using Data.Repositories;
using Job;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timer = Job.Utils.Timer;

static ILogger<T> CreateLogger<T>() where T : class => LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<T>();

var settings = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build()
    .Get<AppSettings>() ?? throw new ApplicationException("Settings malformed.");

var quoteCache = new QuoteRepository(settings.QuoteRepositoryDataPath, CreateLogger<QuoteRepository>());
var returnCache = new ReturnRepository(settings.ReturnRepositoryDataPath, settings.SyntheticReturnsFilePath);

var quotesManager = new QuotesManager(quoteCache, CreateLogger<QuotesManager>());
var returnsManager = new ReturnsManager(quoteCache, returnCache, CreateLogger<ReturnsManager>());
var indicesManager = new IndicesManager(returnCache, CreateLogger<IndicesManager>());

var quoteTickersNeeded = IndicesManager.GetBackfillTickers();

await Timer.Exec("Refresh quotes", quotesManager.RefreshQuotes(quoteTickersNeeded));
await Timer.Exec("Refresh returns", returnsManager.RefreshReturns());
await Timer.Exec("Refresh indices", indicesManager.RefreshIndices());

var ticker = "AVUV";
var result = await GetTickerPerformance(ticker);
result.ToList().ForEach(tick => Console.WriteLine($"{ticker}: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));

await Get();

async Task<IEnumerable<IEnumerable<PerformanceTick>>> Get()
{
    var portfolio = new List<(string ticker, decimal allocation)>()
    {
        ("$^USLCB", 50),
        ("$^USSCB", 50)
    };

    var performance = await GetPortfolioPerformance(portfolio, 100, ReturnPeriod.Daily, new DateTime(2023, 1, 1));

    return performance.ToArray();
}

async Task<IEnumerable<PerformanceTick>> GetTickerPerformance(
    string ticker,
    decimal startingBalance = 100,
    ReturnPeriod granularity = ReturnPeriod.Daily,
    DateTime start = default)
{
    ArgumentNullException.ThrowIfNullOrEmpty(ticker, nameof(ticker));
    ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

    var tickerReturns = await returnCache.Get(ticker, granularity, start);
    var tickerPerformance = GetPerformance(tickerReturns, startingBalance, granularity, start);

    return tickerPerformance.ToArray();
}

IEnumerable<PerformanceTick> GetPerformance(
    IEnumerable<PeriodReturn> tickerReturns,
    decimal startingBalance = 100,
    ReturnPeriod granularity = ReturnPeriod.Daily,
    DateTime start = default)
{
    ArgumentNullException.ThrowIfNull(tickerReturns, nameof(tickerReturns));
    ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

    var performanceTicks = new List<PerformanceTick>();
    var dateRangedTickerReturns = tickerReturns.Where(tick => tick.PeriodStart >= start);

    foreach (var currentReturnTick in dateRangedTickerReturns)
    {
        performanceTicks.Add(new()
        {
            Period = currentReturnTick,
            StartingBalance = startingBalance
        });

        startingBalance = performanceTicks[^1].EndingBalance;
    }

    return performanceTicks;
}

async Task<IEnumerable<IEnumerable<PerformanceTick>>> GetPortfolioPerformance(
    IEnumerable<(string ticker, decimal allocationPercentage)> allocations,
    decimal startingBalance = 100,
    ReturnPeriod granularity = ReturnPeriod.Daily,
    DateTime start = default)
{
    ArgumentNullException.ThrowIfNull(allocations, nameof(allocations));

    if (allocations.Sum(allocation => allocation.allocationPercentage) != 100)
    {
        throw new ArgumentException("Must add up to 100%.", nameof(allocations));
    }

    var returns = await Task.WhenAll(allocations.Select(allocation => returnCache.Get(allocation.ticker, granularity)));
    var latestStart = returns.Select(history => history[0].PeriodStart).Append(start).Max();
    var performances = allocations.Select((allocation, i) =>
        GetPerformance(returns[i], startingBalance * allocation.allocationPercentage / 100, granularity, latestStart));

    return performances.ToArray();
}

readonly struct PerformanceTick
{
    public PeriodReturn Period { get; init; }

    public decimal StartingBalance { get; init; }

    public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

    public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }
}

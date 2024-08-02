using Data.Controllers;
using Data.Models;
using Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    readonly struct PerformanceTick
    {
        public PeriodReturn Period { get; init; }

        public decimal StartingBalance { get; init; }

        public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

        public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }
    }

    static async Task Main(string[] args)
    {
        using var serviceProvider = BuildServiceProvider();

        await RefreshData(serviceProvider);
        await SloppyManualTesting(serviceProvider);
    }

    static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return new ServiceCollection()
            .AddLogging(builder => builder.AddConfiguration(configuration.GetSection("Logging")).AddConsole())
            .Configure<QuoteRepositorySettings>(configuration.GetSection("QuoteRepositorySettings"))
            .Configure<ReturnRepositorySettings>(configuration.GetSection("ReturnRepositorySettings"))
            .AddTransient<IQuoteRepository, QuoteRepository>()
            .AddTransient<IReturnRepository, ReturnRepository>()
            .AddTransient<QuotesManager>()
            .AddTransient<ReturnsManager>()
            .AddTransient<IndicesManager>()
            .BuildServiceProvider();
    }

    static async Task RefreshData(IServiceProvider provider)
    {
        var quotesManager = provider.GetRequiredService<QuotesManager>();
        var returnsManager = provider.GetRequiredService<ReturnsManager>();
        var indicesManager = provider.GetRequiredService<IndicesManager>();
        var quoteTickersNeeded = IndicesManager.GetBackfillTickers();

        await quotesManager.RefreshQuotes(quoteTickersNeeded);
        await returnsManager.RefreshReturns();
        await indicesManager.RefreshIndices();
    }

    static async Task SloppyManualTesting(IServiceProvider provider)
    {
        var returnCache = provider.GetRequiredService<IReturnRepository>();
        var ticker = "AVUV";
        var result = await GetTickerPerformance(returnCache, ticker);
        result.ToList().ForEach(tick => Console.WriteLine($"{ticker}: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));


        var portfolio = new List<(string ticker, decimal allocation)>()
        {
            ("$^USLCB", 50),
            ("$^USSCB", 50)
        };

        var performance = await GetPortfolioPerformance(returnCache, portfolio, 100, ReturnPeriod.Daily, new DateTime(2023, 1, 1));
    }

    static async Task<IEnumerable<PerformanceTick>> GetTickerPerformance(
        IReturnRepository returnCache,
        string ticker,
        decimal startingBalance = 100,
        ReturnPeriod granularity = ReturnPeriod.Daily,
        DateTime start = default)
    {
        ArgumentNullException.ThrowIfNull(returnCache, nameof(returnCache));
        ArgumentNullException.ThrowIfNullOrEmpty(ticker, nameof(ticker));
        ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

        var tickerReturns = await returnCache.Get(ticker, granularity, start);
        var tickerPerformance = GetPerformance(tickerReturns, startingBalance, granularity, start);

        return tickerPerformance.ToArray();
    }

    static IEnumerable<PerformanceTick> GetPerformance(
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

    static async Task<IEnumerable<IEnumerable<PerformanceTick>>> GetPortfolioPerformance(
        IReturnRepository returnCache,
        IEnumerable<(string ticker, decimal allocationPercentage)> allocations,
        decimal startingBalance = 100,
        ReturnPeriod granularity = ReturnPeriod.Daily,
        DateTime start = default)
    {
        ArgumentNullException.ThrowIfNull(returnCache, nameof(returnCache));
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
}
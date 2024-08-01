
using Data.Models;
using Data.Repositories;

public class PerformanceController(ReturnRepository returnRepository, ILogger<PerformanceController> logger)
{
    private ReturnRepository ReturnCache { get; init; } = returnRepository;

    private ILogger<PerformanceController> Logger { get; init; } = logger;

    public async Task Do()
    {
        var perf = await GetPerformance("AVUV") ?? throw new InvalidOperationException();

        perf.ToList().ForEach(tick => Console.WriteLine($"AVUV: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));
    }

    public async Task<IEnumerable<PerformanceTick>> GetPerformance(
        string ticker,
        decimal startingBalance = 100,
        ReturnPeriod granularity = ReturnPeriod.Daily,
        DateTime start = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(ticker, nameof(ticker));
        ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

        var tickerReturns = await ReturnCache.Get(ticker, granularity, start);

        if (tickerReturns == null)
        {
            throw new ArgumentException($"No historical returns found for {ticker}.", nameof(ticker));
        }

        return this.GetPerformance(tickerReturns, startingBalance, granularity, start);
    }

    public IEnumerable<PerformanceTick> GetPerformance(
        IEnumerable<PeriodReturn> tickerReturns,
        decimal startingBalance = 100,
        ReturnPeriod granularity = ReturnPeriod.Daily,
        DateTime start = default)
    {
        ArgumentNullException.ThrowIfNull(tickerReturns, nameof(tickerReturns));
        ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

        var performanceTicks = new List<PerformanceTick>();

        foreach (var currentReturnTick in tickerReturns)
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

    public async Task<IEnumerable<IEnumerable<PerformanceTick>>> GetPerformance(
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

        var returnHistory = await Task.WhenAll(allocations.Select(allocation => ReturnCache.Get(allocation.ticker, granularity) ?? throw new ArgumentException($"No historical returns found for {allocation.ticker}.", nameof(allocations))));

        var actualStart = returnHistory.Select(history => history.First().PeriodStart).Append(start).Max();

        var foo = allocations.Select((allocation, i) => GetPerformance(returnHistory[i], startingBalance * allocation.allocationPercentage, granularity, actualStart));

        return foo;
    }

    public readonly struct PerformanceTick
    {
        public PeriodReturn Period { get; init; }

        public decimal StartingBalance { get; init; }

        public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

        public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }
    }

}

using Data.Models;
using Data.Repositories;

public static class Poc
{
    public static async Task Do(ReturnRepository returnCache)
    {
        var perf = await GetPerformance(returnCache, "AVUV") ?? throw new InvalidOperationException();

        perf.ForEach(tick => Console.WriteLine($"AVUV: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));
    }

    public static async Task<List<PerformanceTick>?> GetPerformance(
        ReturnRepository returnsCache,
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
            performanceTicks.Add(new()
            {
                Period = currentReturnTick,
                StartingBalance = startingBalance
            });

            startingBalance = performanceTicks[^1].EndingBalance;
        }

        return performanceTicks;
    }

    public readonly struct PerformanceTick
    {
        public PeriodReturn Period { get; init; }

        public decimal StartingBalance { get; init; }

        public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

        public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }
    }

}
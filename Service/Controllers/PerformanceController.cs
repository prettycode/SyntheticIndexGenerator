using Data.Models;
using Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Service.Controllers
{
    public readonly struct PerformanceTick
    {
        public PeriodReturn Period { get; init; }

        public decimal StartingBalance { get; init; }

        public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

        public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }
    }

    [ApiController]
    [Route("[controller]")]
    public class PerformanceController(ReturnRepository returnRepository, ILogger<PerformanceController> logger) : ControllerBase
    {
        private ReturnRepository ReturnCache { get; init; } = returnRepository;

        private ILogger<PerformanceController> Logger { get; init; } = logger;

        [HttpGet(Name = "GetTestPortfolioPerformance")]
        public async Task<IEnumerable<IEnumerable<PerformanceTick>>> Get()
        {
            var portfolio = new List<(string ticker, decimal allocation)>()
            {
                ("^USLCB", 50),
                ("^USSCB", 50)
            };

            var performance = await GetPerformance(portfolio, 100, ReturnPeriod.Daily, new DateTime(2023, 1, 1));

            return performance.ToArray();
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
            var tickerPerformance = GetPerformance(tickerReturns, startingBalance, granularity, start);

            return tickerPerformance.ToArray();
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

            var returns = await Task.WhenAll(allocations.Select(allocation => ReturnCache.Get(allocation.ticker, granularity)));
            var latestStart = returns.Select(history => history[0].PeriodStart).Append(start).Max();
            var performances = allocations.Select((allocation, i) =>
                GetPerformance(returns[i], startingBalance * allocation.allocationPercentage, granularity, latestStart));

            return performances.ToArray();
        }
    }
}
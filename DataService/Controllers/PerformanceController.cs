using Data.Models;
using Data.Repositories;
using DataService.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PerformanceController : ControllerBase
    {
        private readonly IReturnRepository returnCache;
        private readonly ILogger<PerformanceController> logger;

        public PerformanceController(IReturnRepository returnCache, ILogger<PerformanceController> logger)
        {
            this.returnCache = returnCache;
            this.logger = logger;
        }

        [HttpGet(Name = "GetTickerPerformanceTest")]
        public async Task<IEnumerable<PerformanceTick>> GetTickerPerformanceTest()
        {
            return await GetTickerPerformance("AVUV");
        }

        [HttpGet(Name = "GetPortfolioPerformanceTest")]
        public async Task<IEnumerable<IEnumerable<PerformanceTick>>> GetPortfolioPerformanceTest()
        {
            var portfolio = new List<(string ticker, decimal allocation)>()
            {
                ("VOO", 50),
                ("AVUV", 50)
            };

            return await GetPortfolioPerformance(portfolio, 100, ReturnPeriod.Daily, new DateTime(2023, 1, 1));
        }

        [HttpGet(Name = "GetTickerPerformance")]
        public async Task<IEnumerable<PerformanceTick>> GetTickerPerformance(
            string ticker,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(ticker, nameof(ticker));
            ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

            var tickerReturns = await returnCache.Get(ticker, granularity, start);
            var tickerPerformance = GetPerformance(tickerReturns, startingBalance, granularity, start, end);

            return tickerPerformance.ToArray();
        }

        // TODO test
        [HttpGet(Name = "GetPortfolioPerformance")]
        public async Task<IEnumerable<IEnumerable<PerformanceTick>>> GetPortfolioPerformance(
            IEnumerable<(string ticker, decimal allocationPercentage)> allocations,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null,
            bool rebalance = false)
        {
            ArgumentNullException.ThrowIfNull(allocations, nameof(allocations));

            if (allocations.Sum(allocation => allocation.allocationPercentage) != 100)
            {
                throw new ArgumentException("Must add up to 100%.", nameof(allocations));
            }

            if (end == null)
            {
                end = DateTime.MaxValue;
            }

            var returns = await Task.WhenAll(allocations.Select(allocation => returnCache.Get(allocation.ticker, granularity)));
            var latestStart = returns.Select(history => history[0].PeriodStart).Append(start).Max();
            var earliestEnd = returns.Select(history => history[^1].PeriodStart).Append(end.Value).Min();

            return allocations
                .Select((allocation, i) => GetPerformance(returns[i], startingBalance * allocation.allocationPercentage / 100, granularity, latestStart, earliestEnd))
                .ToArray();
        }

        // TODO test
        private IEnumerable<PerformanceTick> GetPerformance(
            IEnumerable<PeriodReturn> tickerReturns,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null)
        {
            ArgumentNullException.ThrowIfNull(tickerReturns, nameof(tickerReturns));
            ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

            if (end == null)
            {
                end = DateTime.MaxValue;
            }

            var performanceTicks = new List<PerformanceTick>();
            var dateRangedTickerReturns = tickerReturns.Where(tick => tick.PeriodStart >= start && tick.PeriodStart <= end);

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

    }
}
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
        public async Task<IEnumerable<PeriodDerivedPerformanceTick>> GetTickerPerformanceTest()
        {
            return await GetTickerPerformance("$^USSCV", granularity: ReturnPeriod.Yearly);
        }

        [HttpGet(Name = "GetPortfolioPerformanceTest")]
        public async Task<IEnumerable<PerformanceTick>> GetPortfolioPerformanceTest()
        {
            var portfolio = new List<Allocation>()
            {
                new() { Ticker = "$^USLCB", Percentage = 50 },
                new() { Ticker = "$^USSCV", Percentage = 50 }
            };

            return await GetPortfolioPerformance(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 2, 1));
        }

        [HttpGet(Name = "GetPortfolioPerformanceDecomposedTest")]
        public async Task<IEnumerable<IEnumerable<PeriodDerivedPerformanceTick>>> GetPortfolioPerformanceDecomposedTest()
        {
            var portfolio = new List<Allocation>()
            {
                new() { Ticker = "$^USLCB", Percentage = 50 },
                new() { Ticker = "$^USSCV", Percentage = 50 }
            };

            return await GetPortfolioPerformanceDecomposed(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1));
        }

        [HttpGet(Name = "GetTickerPerformance")]
        public async Task<IEnumerable<PeriodDerivedPerformanceTick>> GetTickerPerformance(
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

            return tickerPerformance;
        }

        [HttpGet(Name = "GetPortfolioPerformanceDecomposed")]
        // TODO test
        public async Task<IEnumerable<IEnumerable<PeriodDerivedPerformanceTick>>> GetPortfolioPerformanceDecomposed(
            IEnumerable<Allocation> allocations,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null)
        {
            ArgumentNullException.ThrowIfNull(allocations, nameof(allocations));

            if (allocations.Sum(allocation => allocation.Percentage) != 100)
            {
                throw new ArgumentException("Must add up to 100%.", nameof(allocations));
            }

            end ??= DateTime.MaxValue;

            var returns = await Task.WhenAll(allocations.Select(allocation => returnCache.Get(allocation.Ticker, granularity)));
            var latestStart = returns.Select(history => history[0].PeriodStart).Append(start).Max();
            var earliestEnd = returns.Select(history => history[^1].PeriodStart).Append(end.Value).Min();
            var performances = allocations.Select((allocation, i) => GetPerformance(returns[i], startingBalance * allocation.Percentage / 100, granularity, latestStart, earliestEnd));

            return performances;
        }

        [HttpGet(Name = "GetPortfolioPerformance")]
        // TODO test
        public async Task<IEnumerable<PerformanceTick>> GetPortfolioPerformance(
            IEnumerable<Allocation> allocations,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null,
            RebalanceStrategy rebalanceStrategy = RebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null)
        {
            var decomposed = await GetPortfolioPerformanceDecomposed(allocations, startingBalance, granularity, start, end);
            var firstDecomposed = decomposed.First();

            if (decomposed.Skip(1).Any(d => d.Count() != firstDecomposed.Count()))
            {
                throw new InvalidOperationException("All decomposed series should (must) have the same length.");
            }

            return firstDecomposed.Select((firstTick, index) => new PerformanceTick
            {
                BalanceIncrease = decomposed.Sum(series => series.ElementAt(index).BalanceIncrease),
                PeriodStart = firstTick.Period.PeriodStart,
                ReturnPeriod = firstTick.Period.ReturnPeriod,
                StartingBalance = decomposed.Sum(series => series.ElementAt(index).StartingBalance)
            });
        }

        // TODO test
        private IEnumerable<PeriodDerivedPerformanceTick> GetPerformance(
            IEnumerable<PeriodReturn> tickerReturns,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null)
        {
            ArgumentNullException.ThrowIfNull(tickerReturns, nameof(tickerReturns));
            ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

            end ??= DateTime.MaxValue;

            var performanceTicks = new List<PeriodDerivedPerformanceTick>();
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
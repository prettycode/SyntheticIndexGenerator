using Data.Models;
using Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DataService.Controllers
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
    public class WeatherForecastController : ControllerBase
    {
        private readonly IReturnRepository returnCache;
        private readonly ILogger<WeatherForecastController> logger;

        public WeatherForecastController(IReturnRepository returnCache, ILogger<WeatherForecastController> logger)
        {
            this.returnCache = returnCache;
            this.logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<IEnumerable<PerformanceTick>>> Get([FromServices] IServiceProvider provider)
        {
            var ticker = "AVUV";
            var result = await GetTickerPerformance(ticker);
            result.ToList().ForEach(tick => Console.WriteLine($"{ticker}: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));

            var portfolio = new List<(string ticker, decimal allocation)>()
            {
                ("$^USLCB", 50),
                ("$^USSCB", 50)
            };

            var performance = await GetPortfolioPerformance(returnCache, portfolio, 100, ReturnPeriod.Daily, new DateTime(2023, 1, 1));

            return performance.ToList();
        }

        private async Task<IEnumerable<PerformanceTick>> GetTickerPerformance(
            string ticker,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null)
        {
            ArgumentNullException.ThrowIfNull(returnCache, nameof(returnCache));
            ArgumentNullException.ThrowIfNullOrEmpty(ticker, nameof(ticker));
            ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

            var tickerReturns = await returnCache.Get(ticker, granularity, start);
            var tickerPerformance = GetPerformance(tickerReturns, startingBalance, granularity, start, end);

            return tickerPerformance.ToArray();
        }

        // TODO test
        private static IEnumerable<PerformanceTick> GetPerformance(
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

        // TODO test
        private static async Task<IEnumerable<IEnumerable<PerformanceTick>>> GetPortfolioPerformance(
            IReturnRepository returnCache,
            IEnumerable<(string ticker, decimal allocationPercentage)> allocations,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime start = default,
            DateTime? end = null,
            bool rebalance = false)
        {
            ArgumentNullException.ThrowIfNull(returnCache, nameof(returnCache));
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
    }
}

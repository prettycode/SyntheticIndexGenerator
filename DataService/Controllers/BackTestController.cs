using Data.Models;
using Data.Repositories;
using DataService.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BackTestController(IReturnRepository returnCache, ILogger<BackTestController> logger) : ControllerBase
    {
        private readonly IReturnRepository returnCache = returnCache;
        private readonly ILogger<BackTestController> logger = logger;

        [HttpGet(Name = "GetPortfolioBackTest_NoRebalance1")]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest_NoRebalance1()
        {
            var portfolio = new List<Allocation>()
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 }
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));
        }

        [HttpGet(Name = "GetPortfolioBackTest_NoRebalance2")]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest_NoRebalance2()
        {
            var portfolio = new List<Allocation>()
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 100 }
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));
        }

        [HttpGet(Name = "GetPortfolioBackTest_NoRebalance3")]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest_NoRebalance3()
        {
            var portfolio = new List<Allocation>()
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));
        }

        [HttpGet(Name = "GetPortfolioBackTest_Rebalance_Monthly1")]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest_Rebalance_Monthly1()
        {
            var portfolio = new List<Allocation>()
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.Monthly);
        }

        [HttpGet(Name = "GetPortfolioBackTest")]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime startDate = default,
            DateTime? endDate = null,
            RebalanceStrategy rebalanceStrategy = RebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null)
        {
            // Validate arguments

            ArgumentNullException.ThrowIfNull(nameof(portfolioConstituents));
            ArgumentOutOfRangeException.ThrowIfZero(portfolioConstituents.Count(), nameof(portfolioConstituents));
            ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

            if (endDate != null)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(endDate.Value, startDate, nameof(endDate));
            }
            else
            {
                endDate = DateTime.MaxValue;
            }

            if ((rebalanceStrategy == RebalanceStrategy.BandsAbsolute ||
                 rebalanceStrategy == RebalanceStrategy.BandsRelative) &&
                (rebalanceBandThreshold == null || rebalanceBandThreshold <= 0))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rebalanceBandThreshold),
                    $"Should be `null` or `0` when `{rebalanceStrategy}` is not " +
                    $"`${nameof(RebalanceStrategy.None)}.{nameof(RebalanceStrategy.None)}`");
            }

            // Get dictionary of portfolio: (Ticker, Percentage)

            var dedupedPortfolioConstituents = portfolioConstituents
                .GroupBy(alloc => alloc.Ticker)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(alloc => alloc.Percentage)
                );

            // Get date-filtered returns history for constituents

            var constituentTickers = dedupedPortfolioConstituents.Keys.ToArray();
            var constituentReturns = await Task.WhenAll(constituentTickers.Select(ticker => returnCache.Get(ticker, granularity, startDate, endDate.Value)));
            var latestStart = constituentReturns.Select(history => history[0].PeriodStart).Append(startDate).Max();
            var earliestEnd = constituentReturns.Select(history => history[^1].PeriodStart).Append(endDate.Value).Min();
            var dateFilteredConstituentReturns = constituentReturns.Select(constituent => constituent.Where(period => period.PeriodStart >= latestStart && period.PeriodStart <= earliestEnd).ToArray());

            // Validate we've filtered them correctly

            var firstConstituentReturns = dateFilteredConstituentReturns.First();

            if (dateFilteredConstituentReturns.Skip(1).Any(d => d.Length != firstConstituentReturns.Length))
            {
                throw new InvalidOperationException("All decomposed series should (must) have the same length.");
            }

            // Organize the date-filtered returns history into dictionary: (Ticker, PeriodReturn[])

            var dateFilteredReturnsByTicker = new Dictionary<string, PeriodReturn[]>();

            for (var i = 0; i < constituentReturns.Length; i++)
            {
                dateFilteredReturnsByTicker[constituentTickers[i]] = dateFilteredConstituentReturns.ElementAt(i);
            }

            // Get dates that require rebalancing and return if results if there's no rebalancing

            var rebalanceDates = GetRebalanceDates(
                firstConstituentReturns.Select(r => r.PeriodStart),
                rebalanceStrategy,
                rebalanceBandThreshold);

            if (rebalanceDates.Length == 0)
            {
                return dateFilteredReturnsByTicker.ToDictionary(
                    pair => pair.Key,
                    pair => GetPeriodReturnsBackTest(pair.Value, startingBalance));
            }

            // Get the rebalanced results

            var backtest = dateFilteredReturnsByTicker.ToDictionary(pair => pair.Key, pair => new List<NominalPeriodReturn>());
            var previousRebalanceDate = latestStart;

            foreach (var rebalanceDate in rebalanceDates)
            {
                foreach (var pair in dateFilteredReturnsByTicker)
                {
                    var ticker = pair.Key;
                    var returns = pair.Value;
                    var dateFilteredReturns = returns.Where(r => r.PeriodStart < rebalanceDate && r.PeriodStart >= previousRebalanceDate).ToArray();
                    var latestBalance = backtest[ticker].Count == 0 ? startingBalance : backtest[ticker][^1].EndingBalance;

                    backtest[ticker].AddRange(GetPeriodReturnsBackTest(dateFilteredReturns, latestBalance));
                }

                previousRebalanceDate = rebalanceDate;
            }

            return backtest.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }

        static DateTime[] GetRebalanceDates(IEnumerable<DateTime> returnPeriodDates, RebalanceStrategy strategy, decimal? bandThreshold)
        {
            if (strategy == RebalanceStrategy.None)
            {
                return new DateTime[0];
            }

            var rebalanceDates = new List<DateTime>();
            DateTime lastRebalanceDate = returnPeriodDates.First();

            for (var i = 1; i < returnPeriodDates.Count(); i++)
            {
                var currentDate = returnPeriodDates.ElementAt(i);
                var isRebalanceNeeded = strategy switch
                {
                    RebalanceStrategy.None => false,
                    RebalanceStrategy.Annually => currentDate >= lastRebalanceDate.AddYears(1),
                    RebalanceStrategy.SemiAnnually => currentDate >= lastRebalanceDate.AddMonths(6),
                    RebalanceStrategy.Quarterly => currentDate >= lastRebalanceDate.AddMonths(3),
                    RebalanceStrategy.Monthly => currentDate >= lastRebalanceDate.AddMonths(1),
                    RebalanceStrategy.Daily => currentDate != lastRebalanceDate,
                    RebalanceStrategy.BandsRelative => throw new NotImplementedException(),
                    RebalanceStrategy.BandsAbsolute => throw new NotImplementedException(),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategy))
                };

                if (!isRebalanceNeeded)
                {
                    continue;
                }

                rebalanceDates.Add(currentDate);
            }

            return rebalanceDates.ToArray();
        }

        private async Task<NominalPeriodReturn[]> GetTickerBackTest(
            string ticker,
            decimal startingBalance,
            ReturnPeriod granularity,
            DateTime startDate,
            DateTime endDate)
        {
            var tickerReturns = await returnCache.Get(ticker, granularity, startDate, endDate);
            return GetPeriodReturnsBackTest([.. tickerReturns], startingBalance);
        }

        private static NominalPeriodReturn[] GetPeriodReturnsBackTest(PeriodReturn[] tickerReturns, decimal startingBalance)
        {
            var performanceTicks = new List<NominalPeriodReturn>();
            var currentBalance = startingBalance;

            foreach (var currentReturnTick in tickerReturns)
            {
                performanceTicks.Add(new(currentReturnTick.SourceTicker, currentBalance, currentReturnTick));
                currentBalance = performanceTicks[^1].EndingBalance;
            }

            return performanceTicks.ToArray();
        }
    }
}
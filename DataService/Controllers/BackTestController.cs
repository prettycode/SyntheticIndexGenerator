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

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest_NoRebalance1_SingleConstituent1()
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 100 }
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));
        }

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>>
            GetPortfolioBackTest_NoRebalance1_DuplicateConstituent2()
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 }
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));
        }

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>>
            GetPortfolioBackTest_NoRebalance_MultipleDifferentConstituents1()
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));
        }

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest_Rebalance_Monthly1()
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            return await GetPortfolioBackTest(portfolio, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1), RebalanceStrategy.Monthly);
        }

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBackTest(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime startDate = default,
            DateTime? endDate = null,
            RebalanceStrategy rebalanceStrategy = RebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null)
        {
            ValidateArguments(portfolioConstituents, startingBalance, startDate, endDate, rebalanceStrategy,
                rebalanceBandThreshold);

            var endDateValue = endDate ?? DateTime.MaxValue;
            var dedupedPortfolioConstituents = portfolioConstituents
                .GroupBy(alloc => alloc.Ticker)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(alloc => alloc.Percentage)
                );

            var constituentTickers = dedupedPortfolioConstituents.Keys.ToArray();
            var constituentReturns = await Task.WhenAll(constituentTickers.Select(ticker =>
                returnCache.Get(ticker, granularity, startDate, endDateValue)));

            var latestStart = constituentReturns.Select(history => history.First().PeriodStart).Append(startDate).Max();
            var earliestEnd = constituentReturns.Select(history => history.Last().PeriodStart).Append(endDateValue).Min();

            var dateFilteredReturnsByTicker = constituentTickers
                .Zip(constituentReturns, (ticker, returns) => new { ticker, returns })
                .ToDictionary(
                    x => x.ticker,
                    x => x.returns.Where(period => period.PeriodStart >= latestStart && period.PeriodStart <= earliestEnd)
                        .ToArray()
                );

            ValidateFilteredReturns(dateFilteredReturnsByTicker.Values);

            var rebalanceDates = GetRebalanceDates(
                dateFilteredReturnsByTicker.Values.First().Select(r => r.PeriodStart),
                rebalanceStrategy,
                rebalanceBandThreshold);

            if (!rebalanceDates.Any())
            {
                return dateFilteredReturnsByTicker.ToDictionary(
                    pair => pair.Key,
                    pair => GetPeriodReturnsBackTest(pair.Value,
                        startingBalance * (dedupedPortfolioConstituents[pair.Key] / 100))
                );
            }

            return PerformBacktestWithRebalancing(dateFilteredReturnsByTicker, dedupedPortfolioConstituents,
                startingBalance, rebalanceDates, latestStart);
        }

        private void ValidateArguments(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance,
            DateTime startDate,
            DateTime? endDate,
            RebalanceStrategy rebalanceStrategy,
            decimal? rebalanceBandThreshold)
        {
            ArgumentNullException.ThrowIfNull(portfolioConstituents);

            if (!portfolioConstituents.Any())
            {
                throw new ArgumentException("Portfolio constituents cannot be empty", nameof(portfolioConstituents));
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

            if (endDate.HasValue && endDate.Value < startDate)
            {
                throw new ArgumentOutOfRangeException(nameof(endDate), "End date must be after start date");
            }

            if ((rebalanceStrategy == RebalanceStrategy.BandsAbsolute ||
                 rebalanceStrategy == RebalanceStrategy.BandsRelative)
                && (!rebalanceBandThreshold.HasValue || rebalanceBandThreshold <= 0))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rebalanceBandThreshold),
                    $"Should be greater than 0 when rebalance strategy is {rebalanceStrategy}");
            }
        }

        private void ValidateFilteredReturns(IEnumerable<PeriodReturn[]> dateFilteredConstituentReturns)
        {
            if (!dateFilteredConstituentReturns.Any())
            {
                throw new ArgumentException("The collection of filtered returns is empty.",
                    nameof(dateFilteredConstituentReturns));
            }

            var firstReturns = dateFilteredConstituentReturns.First();
            var firstLength = firstReturns.Length;

            if (dateFilteredConstituentReturns.Any(d => d.Length != firstLength))
            {
                throw new InvalidOperationException("All decomposed series should have the same length.");
            }

            var firstDates = firstReturns.Select(period => period.PeriodStart).ToArray();

            foreach (var returns in dateFilteredConstituentReturns.Skip(1))
            {
                var currentDates = returns.Select(period => period.PeriodStart).ToArray();

                if (!firstDates.SequenceEqual(currentDates))
                {
                    throw new InvalidOperationException("All decomposed series should have identical DateTime arrays.");
                }
            }
        }

        private Dictionary<string, NominalPeriodReturn[]> PerformBacktestWithRebalancing(
            Dictionary<string, PeriodReturn[]> dateFilteredReturnsByTicker,
            Dictionary<string, decimal> dedupedPortfolioConstituents,
            decimal startingBalance,
            IEnumerable<DateTime> rebalanceDates,
            DateTime latestStart)
        {
            var backtest = dateFilteredReturnsByTicker.ToDictionary(pair => pair.Key, pair => new List<NominalPeriodReturn>());
            var rebalancedBalances = dedupedPortfolioConstituents.ToDictionary(pair => pair.Key,
                pair => startingBalance * (pair.Value / 100));

            var previousRebalanceDate = latestStart;
            foreach (var rebalanceDate in rebalanceDates)
            {
                foreach (var (ticker, returns) in dateFilteredReturnsByTicker)
                {
                    var dateFilteredReturns = returns.Where(r =>
                        r.PeriodStart < rebalanceDate && r.PeriodStart >= previousRebalanceDate).ToArray();
                    backtest[ticker].AddRange(GetPeriodReturnsBackTest(dateFilteredReturns, rebalancedBalances[ticker]));
                }

                previousRebalanceDate = rebalanceDate;

                var totalBalance = backtest.Sum(pair => pair.Value.Last().EndingBalance);
                rebalancedBalances = dedupedPortfolioConstituents.ToDictionary(pair => pair.Key,
                    pair => totalBalance * (pair.Value / 100));
            }

            return backtest.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }

        static DateTime[] GetRebalanceDates(IEnumerable<DateTime> returnPeriodDates, RebalanceStrategy strategy,
            decimal? bandThreshold)
        {
            if (strategy == RebalanceStrategy.None)
            {
                return Array.Empty<DateTime>();
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
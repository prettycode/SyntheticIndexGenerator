using System.Linq;
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
        public async Task<PortfolioBackTest> GetPortfolioBackTest(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance = 100,
            ReturnPeriod periodType = ReturnPeriod.Daily,
            DateTime firstPeriod = default,
            DateTime? lastPeriod = null,
            RebalanceStrategy rebalanceStrategy = RebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null)
        {
            ValidateArguments(
                portfolioConstituents,
                startingBalance,
                firstPeriod,
                lastPeriod,
                rebalanceStrategy,
                rebalanceBandThreshold);

            lastPeriod ??= DateTime.MaxValue;

            var (decomposed, rebalances) = await GetPortfolioBackTestDecomposed(
                portfolioConstituents,
                startingBalance,
                periodType,
                firstPeriod,
                lastPeriod.Value,
                rebalanceStrategy,
                rebalanceBandThreshold);

            var aggregated = GetPortfolioBackTestDecomposedRollup(decomposed);
            var backtest = new PortfolioBackTest()
            {
                AggregatePerformance = aggregated,
                DecomposedPerformanceByTicker = decomposed,
                RebalancesByTicker = rebalances,
                RebalanceStrategy = rebalanceStrategy,
                RebalanceThreshold = rebalanceBandThreshold
            };

            return backtest;
        }

        private static NominalPeriodReturn[] GetPortfolioBackTestDecomposedRollup(
            Dictionary<string, NominalPeriodReturn[]> decomposedBackTest)
        {
            static decimal CalculateReturnPercentage(
                Dictionary<string, NominalPeriodReturn[]> decomposedBackTest,
                int currentPeriod)
            {
                var rollupStartingBalance = decomposedBackTest.Sum(pair => pair.Value[currentPeriod].StartingBalance);
                var rollupEndingBalance = decomposedBackTest.Sum(pair => pair.Value[currentPeriod].EndingBalance);

                return ((rollupEndingBalance / rollupStartingBalance) - 1) * 100;
            }

            var firstTickerBackTest = decomposedBackTest.Values.First();

            return firstTickerBackTest
                .Select((_, currentPeriod) => new NominalPeriodReturn
                {
                    PeriodStart = firstTickerBackTest[currentPeriod].PeriodStart,
                    ReturnPeriod = firstTickerBackTest[currentPeriod].ReturnPeriod,
                    Ticker = null!,
                    StartingBalance = decomposedBackTest.Sum(pair => pair.Value[currentPeriod].StartingBalance),
                    ReturnPercentage = CalculateReturnPercentage(decomposedBackTest, currentPeriod)
                })
                .ToArray();
        }

        private async Task<(Dictionary<string, NominalPeriodReturn[]>, Dictionary<string, RebalanceEvent[]>)> GetPortfolioBackTestDecomposed(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance,
            ReturnPeriod periodType,
            DateTime firstPeriod,
            DateTime lastPeriod,
            RebalanceStrategy rebalanceStrategy,
            decimal? rebalanceBandThreshold)
        {
            var dedupedPortfolioConstituents = portfolioConstituents
                .GroupBy(alloc => alloc.Ticker)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(alloc => alloc.Percentage)
                );

            var constituentTickers = dedupedPortfolioConstituents.Keys.ToArray();

            var constituentReturns = await Task.WhenAll(
                constituentTickers.Select(ticker => returnCache.Get(ticker, periodType, firstPeriod, lastPeriod)));

            var firstSharedFirstPeriod = constituentReturns
                .Select(history => history.First().PeriodStart)
                .Append(firstPeriod)
                .Max();

            var lastSharedLastPeriod = constituentReturns
                .Select(history => history.Last().PeriodStart)
                .Append(lastPeriod)
                .Min();

            var dateFilteredReturnsByTicker = constituentTickers
                .Zip(constituentReturns, (ticker, returns) => new { ticker, returns })
                .ToDictionary(
                    x => x.ticker,
                    x => x.returns
                        .Where(period => period.PeriodStart >= firstSharedFirstPeriod && period.PeriodStart <= lastSharedLastPeriod)
                        .ToArray()
                );

            ValidateReturnCollectionHomogeneity(dateFilteredReturnsByTicker.Values);

            if (rebalanceStrategy == RebalanceStrategy.None)
            {
                var unbalancedPerformance = dateFilteredReturnsByTicker.ToDictionary(
                    pair => pair.Key,
                    pair => GetPeriodReturnsBackTest(pair.Value,
                        startingBalance * (dedupedPortfolioConstituents[pair.Key] / 100))
                );

                return (unbalancedPerformance, []);
            }

            var rebalancedPerformance = GetRebalancedPortfolioBacktest(
                dateFilteredReturnsByTicker,
                dedupedPortfolioConstituents,
                startingBalance,
                rebalanceStrategy,
                rebalanceBandThreshold
            );

            return rebalancedPerformance;
        }

        private static void ValidateArguments(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance,
            DateTime firstPeriod,
            DateTime? lastPeriod,
            RebalanceStrategy rebalanceStrategy,
            decimal? rebalanceBandThreshold)
        {
            ArgumentNullException.ThrowIfNull(portfolioConstituents);

            if (!portfolioConstituents.Any())
            {
                throw new ArgumentException("Portfolio constituents cannot be empty", nameof(portfolioConstituents));
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

            if (lastPeriod.HasValue && lastPeriod.Value < firstPeriod)
            {
                throw new ArgumentOutOfRangeException(nameof(lastPeriod), "Last period cannot be before first period.");
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

        private static void ValidateReturnCollectionHomogeneity(IEnumerable<PeriodReturn[]> dateFilteredConstituentReturns)
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
                throw new InvalidOperationException("All constituent histories should have the same length.");
            }

            var firstDates = firstReturns.Select(period => period.PeriodStart).ToArray();

            foreach (var returns in dateFilteredConstituentReturns.Skip(1))
            {
                var currentDates = returns.Select(period => period.PeriodStart).ToArray();

                if (!firstDates.SequenceEqual(currentDates))
                {
                    throw new InvalidOperationException("All constituent histories should have identical dates.");
                }
            }
        }

        private static (Dictionary<string, NominalPeriodReturn[]>, Dictionary<string, RebalanceEvent[]>) GetRebalancedPortfolioBacktest(
            Dictionary<string, PeriodReturn[]> dateFilteredReturnsByTicker,
            Dictionary<string, decimal> targetAllocationsByTicker,
            decimal startingBalance,
            RebalanceStrategy strategy,
            decimal? threshold)
        {
            static bool IsOutsideRelativeBands(
                Dictionary<string, decimal> targetAllocations,
                Dictionary<string, decimal> currentAllocations,
                decimal threshold)
            {
                return targetAllocations.Any(kvp =>
                    Math.Abs(currentAllocations[kvp.Key] - kvp.Value) / kvp.Value > threshold);
            }

            static bool IsOutsideAbsoluteBands(
                Dictionary<string, decimal> targetAllocations,
                Dictionary<string, decimal> currentAllocations,
                decimal threshold)
            {
                return targetAllocations.Any(kvp =>
                    Math.Abs(currentAllocations[kvp.Key] - kvp.Value) > threshold);
            }

            static Dictionary<string, decimal> GetEndingAllocationsByTicker(Dictionary<string, List<NominalPeriodReturn>> backtest)
            {
                var currentBalancesByTicker = backtest.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value[^1].EndingBalance
                );

                var currentTotalBalance = currentBalancesByTicker.Sum(pair => pair.Value);

                var currentAllocationsByTicker = currentBalancesByTicker.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value / currentTotalBalance) * 100
                );

                return currentAllocationsByTicker;
            }

            static bool IsBandedRebalanceNeeded(
                Dictionary<string, decimal> targetAllocationsByTicker,
                Dictionary<string, decimal> currentAllocationsByTicker,
                RebalanceStrategy strategy,
                decimal rebalanceBandThreshold) => strategy switch
                {
                    RebalanceStrategy.BandsRelative => IsOutsideRelativeBands(
                        targetAllocationsByTicker,
                        currentAllocationsByTicker,
                        rebalanceBandThreshold),
                    RebalanceStrategy.BandsAbsolute => IsOutsideAbsoluteBands(
                        targetAllocationsByTicker,
                        currentAllocationsByTicker,
                        rebalanceBandThreshold),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategy))
                };

            static bool IsPeriodicRebalanceNeeded(
                Dictionary<string, decimal> targetAllocationsByTicker,
                Dictionary<string, decimal> currentAllocationsByTicker,
                DateTime nextPeriodStartDate,
                DateTime lastRebalanceStartDate,
                RebalanceStrategy strategy)
            {
                return strategy switch
                {
                    RebalanceStrategy.Annually => nextPeriodStartDate >= lastRebalanceStartDate.AddYears(1),
                    RebalanceStrategy.SemiAnnually => nextPeriodStartDate >= lastRebalanceStartDate.AddMonths(6),
                    RebalanceStrategy.Quarterly => nextPeriodStartDate >= lastRebalanceStartDate.AddMonths(3),
                    RebalanceStrategy.Monthly => nextPeriodStartDate >= lastRebalanceStartDate.AddMonths(1),
                    RebalanceStrategy.Weekly => nextPeriodStartDate >= lastRebalanceStartDate.AddDays(7),
                    RebalanceStrategy.Daily => nextPeriodStartDate >= lastRebalanceStartDate.AddDays(1),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategy))
                };
            }

            var backtest = dateFilteredReturnsByTicker.ToDictionary(pair => pair.Key, pair
                => new List<NominalPeriodReturn>());

            var rebalances = dateFilteredReturnsByTicker.ToDictionary(pair => pair.Key, pair
                => new List<RebalanceEvent>());

            var currentTotalBalanceByTicker = targetAllocationsByTicker.ToDictionary(pair
                => pair.Key, pair => startingBalance * (pair.Value / 100));

            var historyPeriodsCount = dateFilteredReturnsByTicker.First().Value.Length;
            var lastRebalanceStartDate = dateFilteredReturnsByTicker.First().Value[0].PeriodStart;

            for (var i = 0; i < historyPeriodsCount; i++)
            {
                foreach (var (ticker, returns) in dateFilteredReturnsByTicker)
                {
                    var tickerCurrentTotalBalance = currentTotalBalanceByTicker[ticker];
                    var periodReturn = returns[i];

                    backtest[ticker].AddRange(GetPeriodReturnsBackTest([periodReturn], tickerCurrentTotalBalance));

                    currentTotalBalanceByTicker = targetAllocationsByTicker.ToDictionary(pair => pair.Key, pair
                        => backtest[ticker][^1].EndingBalance);
                }

                if (i == historyPeriodsCount - 1)
                {
                    break;
                }

                var nextPeriodStartDate = dateFilteredReturnsByTicker.First().Value.ElementAt(i + 1).PeriodStart;
                var currentAllocationsByTicker = GetEndingAllocationsByTicker(backtest);

                if ((strategy == RebalanceStrategy.BandsAbsolute || strategy == RebalanceStrategy.BandsRelative) &&
                    !IsBandedRebalanceNeeded(targetAllocationsByTicker, currentAllocationsByTicker, strategy, threshold.Value))
                {
                    continue;
                }

                if (!(strategy == RebalanceStrategy.BandsAbsolute || strategy == RebalanceStrategy.BandsRelative) &&
                    !IsPeriodicRebalanceNeeded(targetAllocationsByTicker, currentAllocationsByTicker, nextPeriodStartDate, lastRebalanceStartDate, strategy))
                {
                    continue;
                }

                var totalPortfolioBalance = backtest.Sum(pair => pair.Value.Last().EndingBalance);

                foreach (var (ticker, returns) in backtest)
                {
                    var lastTick = returns[^1];
                    var lastTickEndingBalance = lastTick.EndingBalance;
                    var balanceAfterRebalance = totalPortfolioBalance * (targetAllocationsByTicker[ticker] / 100);

                    rebalances[ticker].Add(new RebalanceEvent()
                    {
                        Ticker = ticker,
                        PrecedingCompletedPeriodStart = lastTick.PeriodStart,
                        PrecedingCompletedPeriodType = lastTick.ReturnPeriod,
                        BalanceBeforeRebalance = lastTickEndingBalance,
                        BalanceAfterRebalance = balanceAfterRebalance
                    });

                    currentTotalBalanceByTicker[ticker] = balanceAfterRebalance;
                }

                lastRebalanceStartDate = nextPeriodStartDate;
            }

            return (
                backtest.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()),
                rebalances.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray())
            );
        }

        private static NominalPeriodReturn[] GetPeriodReturnsBackTest(
            PeriodReturn[] tickerReturns,
            decimal startingBalance)
        {
            var performanceTicks = new List<NominalPeriodReturn>();
            var currentBalance = startingBalance;

            foreach (var currentReturnTick in tickerReturns)
            {
                performanceTicks.Add(new(currentReturnTick.SourceTicker, currentBalance, currentReturnTick));
                currentBalance = performanceTicks[^1].EndingBalance;
            }

            return [.. performanceTicks];
        }
    }
}
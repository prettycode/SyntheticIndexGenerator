using Data.Controllers;
using Data.Models;
using DataService.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackTestController(IQuotesService quotesService, IReturnsService returnsService, ILogger<BackTestController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<PortfolioBackTest> GetPortfolioBackTest(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance = 100,
            PeriodType periodType = PeriodType.Daily,
            DateTime firstPeriod = default,
            DateTime? lastPeriod = null,
            RebalanceStrategy rebalanceStrategy = RebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null)
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

            lastPeriod ??= DateTime.MaxValue;

            var (decomposed, rebalances) = await GetPortfolioBackTestDecomposed(
                portfolioConstituents,
                startingBalance,
                periodType,
                firstPeriod,
                lastPeriod.Value,
                rebalanceStrategy,
                rebalanceBandThreshold);

            var aggregated = AggregateDecomposedPortfolioBackTest(decomposed);
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

        private static NominalPeriodReturn[] AggregateDecomposedPortfolioBackTest(
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
                    PeriodType = firstTickerBackTest[currentPeriod].PeriodType,
                    Ticker = null!,
                    StartingBalance = decomposedBackTest.Sum(pair => pair.Value[currentPeriod].StartingBalance),
                    ReturnPercentage = CalculateReturnPercentage(decomposedBackTest, currentPeriod)
                })
                .ToArray();
        }

        private async Task<IEnumerable<PeriodReturn[]>> GetTickerReturns(
            HashSet<string> tickers,
            PeriodType periodType)
        {
            static bool IsSyntheticTicker(string ticker) => ticker.StartsWith('$') || ticker.StartsWith('#');

            var nonSyntheticTickers = tickers
                .Where(ticker => !IsSyntheticTicker(ticker))
                .ToHashSet();

            var syntheticTickers = tickers
                .Where(ticker => IsSyntheticTicker(ticker))
                .ToHashSet();

            var nonSyntheticPricesByTicker = await quotesService.GetPrices(nonSyntheticTickers);
            var nonSyntheticReturnsByTicker = await returnsService.GetReturns(nonSyntheticPricesByTicker);
            var syntheticReturnsByTicker = await returnsService.GetSyntheticReturns(syntheticTickers);
            var constituentReturnsByTicker = nonSyntheticReturnsByTicker.Union(syntheticReturnsByTicker);

            var periodReturnsByTicker = constituentReturnsByTicker.ToDictionary(
                pair => pair.Key,
                pair => pair.Value[periodType]);

            return periodReturnsByTicker.Values;
        }

        private async Task<Dictionary<string, PeriodReturn[]>> GetDateRangedReturns(
            HashSet<string> tickers,
            PeriodType periodType,
            DateTime firstPeriod,
            DateTime lastPeriod)
        {
            var constituentReturns = await GetTickerReturns(tickers, periodType);

            var firstSharedFirstPeriod = constituentReturns
                .Select(history => history.First().PeriodStart)
                .Append(firstPeriod)
                .Max();

            var lastSharedLastPeriod = constituentReturns
                .Select(history => history.Last().PeriodStart)
                .Append(lastPeriod)
                .Min();

            var dateFilteredReturnsByTicker = tickers
                .Zip(constituentReturns, (ticker, returns) => new { ticker, returns })
                .ToDictionary(
                    x => x.ticker,
                    x => x.returns
                        .Where(period => period.PeriodStart >= firstSharedFirstPeriod && period.PeriodStart <= lastSharedLastPeriod)
                        .ToArray()
                );

            return dateFilteredReturnsByTicker;
        }

        private async Task<(Dictionary<string, NominalPeriodReturn[]>, Dictionary<string, RebalanceEvent[]>)> GetPortfolioBackTestDecomposed(
            IEnumerable<Allocation> portfolioConstituents,
            decimal startingBalance,
            PeriodType periodType,
            DateTime firstPeriod,
            DateTime lastPeriod,
            RebalanceStrategy rebalanceStrategy,
            decimal? rebalanceBandThreshold)
        {
            static void ConfirmAlignment(IEnumerable<PeriodReturn[]> dateFilteredConstituentReturns)
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

            var dedupedPortfolioConstituents = portfolioConstituents
                .GroupBy(alloc => alloc.Ticker)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(alloc => alloc.Percentage)
                );

            var dateFilteredReturnsByTicker = await GetDateRangedReturns(
                new(dedupedPortfolioConstituents.Keys),
                periodType,
                firstPeriod,
                lastPeriod);

            var rebalancedPerformance = GetRebalancedPortfolioBacktest(
                dateFilteredReturnsByTicker,
                dedupedPortfolioConstituents,
                startingBalance,
                rebalanceStrategy,
                rebalanceBandThreshold
            );

            ConfirmAlignment(dateFilteredReturnsByTicker.Values);

            return rebalancedPerformance;
        }

        private static (Dictionary<string, NominalPeriodReturn[]>, Dictionary<string, RebalanceEvent[]>) GetRebalancedPortfolioBacktest(
            Dictionary<string, PeriodReturn[]> periodAlignedReturnsByTicker,
            Dictionary<string, decimal> targetAllocationsByTicker,
            decimal startingBalance,
            RebalanceStrategy strategy,
            decimal? threshold)
        {
            static bool IsOutsideRelativeBands(
                Dictionary<string, decimal> targetAllocations,
                Dictionary<string, decimal> currentAllocations,
                decimal threshold)
                => targetAllocations.Any(kvp =>
                    Math.Abs((currentAllocations[kvp.Key] - kvp.Value) / kvp.Value) * 100 >= threshold);

            static bool IsOutsideAbsoluteBands(
                Dictionary<string, decimal> targetAllocations,
                Dictionary<string, decimal> currentAllocations,
                decimal threshold)
                => targetAllocations.Any(kvp =>
                    Math.Abs(currentAllocations[kvp.Key] - kvp.Value) > threshold);

            static Dictionary<string, decimal> GetEndingAllocationsByTicker(Dictionary<string, List<NominalPeriodReturn>> backtest)
            {
                var currentBalancesByTicker = backtest.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value[^1].EndingBalance
                );

                var currentTotalBalance = currentBalancesByTicker.Values.Sum();

                return currentBalancesByTicker.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value / currentTotalBalance) * 100
                );
            }

            static bool IsBandedRebalanceNeeded(
                Dictionary<string, decimal> targetAllocationsByTicker,
                Dictionary<string, decimal> currentAllocationsByTicker,
                RebalanceStrategy strategy,
                decimal? threshold) => strategy switch
                {
                    RebalanceStrategy.BandsRelative => IsOutsideRelativeBands(
                        targetAllocationsByTicker,
                        currentAllocationsByTicker,
                        threshold ?? throw new ArgumentNullException(nameof(threshold))),
                    RebalanceStrategy.BandsAbsolute => IsOutsideAbsoluteBands(
                        targetAllocationsByTicker,
                        currentAllocationsByTicker,
                        threshold ?? throw new ArgumentNullException(nameof(threshold))),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategy))
                };

            static bool IsPeriodicRebalanceNeeded(
                DateTime nextPeriodStartDate,
                DateTime lastRebalancedStartDate,
                RebalanceStrategy strategy)
                => strategy switch
                {
                    RebalanceStrategy.Annually => nextPeriodStartDate >= lastRebalancedStartDate.AddYears(1),
                    RebalanceStrategy.SemiAnnually => nextPeriodStartDate >= lastRebalancedStartDate.AddMonths(6),
                    RebalanceStrategy.Quarterly => nextPeriodStartDate >= lastRebalancedStartDate.AddMonths(3),
                    RebalanceStrategy.Monthly => nextPeriodStartDate >= lastRebalancedStartDate.AddMonths(1),
                    RebalanceStrategy.Weekly => nextPeriodStartDate >= lastRebalancedStartDate.AddDays(7),
                    RebalanceStrategy.Daily => nextPeriodStartDate >= lastRebalancedStartDate.AddDays(1),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategy))
                };

            var backtest = periodAlignedReturnsByTicker.ToDictionary(pair => pair.Key, _ => new List<NominalPeriodReturn>());
            var rebalances = periodAlignedReturnsByTicker.ToDictionary(pair => pair.Key, _ => new List<RebalanceEvent>());
            var currentTotalBalanceByTicker = targetAllocationsByTicker.ToDictionary(
                pair => pair.Key,
                pair => startingBalance * (pair.Value / 100m));

            var (firstTicker, firstTickerReturns) = periodAlignedReturnsByTicker.First();
            var returnsCount = firstTickerReturns.Length;
            var lastRebalancedStartDate = firstTickerReturns[0].PeriodStart;

            for (var i = 0; i < returnsCount; i++)
            {
                foreach (var (ticker, returns) in periodAlignedReturnsByTicker)
                {
                    var tickerCurrentTotalBalance = currentTotalBalanceByTicker[ticker];
                    var periodReturn = returns[i];

                    backtest[ticker].AddRange(GetPeriodReturnsBackTest([periodReturn], tickerCurrentTotalBalance));

                    currentTotalBalanceByTicker[ticker] = backtest[ticker][^1].EndingBalance;
                }

                if (i == returnsCount - 1)
                {
                    break;
                }

                var currentAllocationsByTicker = GetEndingAllocationsByTicker(backtest);
                var nextPeriodStartDate = firstTickerReturns[i + 1].PeriodStart;

                bool needsRebalancing = strategy switch
                {
                    RebalanceStrategy.None => false,
                    RebalanceStrategy.BandsAbsolute or RebalanceStrategy.BandsRelative =>
                        IsBandedRebalanceNeeded(targetAllocationsByTicker, currentAllocationsByTicker, strategy, threshold),
                    _ => IsPeriodicRebalanceNeeded(nextPeriodStartDate, lastRebalancedStartDate, strategy)
                };

                if (!needsRebalancing)
                {
                    continue;
                }

                lastRebalancedStartDate = nextPeriodStartDate;

                var totalPortfolioBalance = backtest.Sum(pair => pair.Value[^1].EndingBalance);

                foreach (var (ticker, returns) in backtest)
                {
                    var lastTick = returns[^1];
                    var balanceAfterRebalance = totalPortfolioBalance * (targetAllocationsByTicker[ticker] / 100m);

                    rebalances[ticker].Add(new RebalanceEvent
                    {
                        Ticker = ticker,
                        PrecedingCompletedPeriodStart = lastTick.PeriodStart,
                        PrecedingCompletedPeriodType = lastTick.PeriodType,
                        BalanceBeforeRebalance = lastTick.EndingBalance,
                        BalanceAfterRebalance = balanceAfterRebalance
                    });

                    currentTotalBalanceByTicker[ticker] = balanceAfterRebalance;
                }
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
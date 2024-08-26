using Data.Returns;
using Microsoft.Extensions.Logging;

namespace Data.BackTest;

internal class BackTestService(IReturnsService returnsService, ILogger<BackTestService> logger) : IBackTestService
{
    public async Task<BackTest> GetPortfolioBackTest(
        IEnumerable<BackTestAllocation> portfolioConstituents,
        decimal startingBalance = 100,
        PeriodType periodType = PeriodType.Daily,
        DateTime firstPeriod = default,
        DateTime? lastPeriod = null,
        BackTestRebalanceStrategy rebalanceStrategy = BackTestRebalanceStrategy.None,
        decimal? rebalanceBandThreshold = null,
        bool includeIncompleteEndingPeriod = true)
    {
        ArgumentNullException.ThrowIfNull(portfolioConstituents);

        if (!portfolioConstituents.Any())
        {
            throw new ArgumentException("Portfolio constituents cannot be empty.", nameof(portfolioConstituents));
        }

        if (portfolioConstituents.Sum(constituent => constituent.Percentage) != 100)
        {
            throw new ArgumentException("Portfolio constituents percentages must add up to 100.", nameof(portfolioConstituents));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance, 1, nameof(startingBalance));

        if (lastPeriod.HasValue && lastPeriod.Value < firstPeriod)
        {
            throw new ArgumentOutOfRangeException(nameof(lastPeriod), "Last period cannot be before first period.");
        }

        if ((rebalanceStrategy == BackTestRebalanceStrategy.BandsAbsolute ||
             rebalanceStrategy == BackTestRebalanceStrategy.BandsRelative)
            && (!rebalanceBandThreshold.HasValue || rebalanceBandThreshold <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rebalanceBandThreshold),
                $"Should be greater than 0 when rebalance strategy is {rebalanceStrategy}");
        }

        if (rebalanceStrategy != BackTestRebalanceStrategy.None &&
            ((periodType >= PeriodType.Yearly && rebalanceStrategy < BackTestRebalanceStrategy.Annually) ||
            periodType >= PeriodType.Monthly && rebalanceStrategy < BackTestRebalanceStrategy.Monthly))
        {
            throw new ArgumentOutOfRangeException(nameof(rebalanceStrategy),
                "Rebalance strategy cannot be more frequent than period type.");
        }

        lastPeriod ??= DateTime.MaxValue;

        var (decomposed, rebalances) = await GetPortfolioBackTestDecomposed(
            portfolioConstituents,
            startingBalance,
            periodType,
            firstPeriod,
            lastPeriod.Value,
            rebalanceStrategy,
            rebalanceBandThreshold,
            includeIncompleteEndingPeriod);

        var aggregated = AggregateDecomposedPortfolioBackTest(decomposed);
        var aggregatedDrawdownReturns = GetBackTestPeriodReturnDrawdownReturns(aggregated);
        var aggregatedDrawdownPeriods = GetBackTestPeriodReturnDrawdownPeriods(aggregated);
        var backtest = new BackTest()
        {
            AggregatePerformance = aggregated,
            AggregatePerformanceDrawdownsReturns = aggregatedDrawdownReturns,
            AggregatePerformanceDrawdownPeriods = aggregatedDrawdownPeriods,
            DecomposedPerformanceByTicker = decomposed,
            RebalancesByTicker = rebalances,
            RebalanceStrategy = rebalanceStrategy,
            RebalanceThreshold = rebalanceBandThreshold
        };

        return backtest;
    }

    private static BackTestDrawdownPeriod[] GetBackTestPeriodReturnDrawdownPeriods(BackTestPeriodReturn[] returns)
    {
        var result = new List<BackTestDrawdownPeriod>();

        var returnsCount = returns.Length;
        var drawdownStartingBalance = returns[0].StartingBalance;
        var inDrawdown = returns[0].ReturnPercentage < 0;
        DateTime? drawdownFirstPeriodStart = !inDrawdown ? null : returns[0].PeriodStart;

        for (var i = 0; i < returnsCount; i++)
        {
            var wasInDrawdown = inDrawdown;
            var currentReturn = returns[i];

            inDrawdown = currentReturn.EndingBalance < drawdownStartingBalance;

            if (!inDrawdown)
            {
                drawdownStartingBalance = currentReturn.EndingBalance;
            }

            // Drawdown or positive returns continue
            if ((wasInDrawdown && inDrawdown) || (!wasInDrawdown && !inDrawdown))
            {
                continue;
            }

            // Drawdown ended
            if ((wasInDrawdown && !inDrawdown) || (inDrawdown && i == returnsCount - 1))
            {
                if (drawdownFirstPeriodStart == null)
                {
                    throw new InvalidOperationException("Drawdown first period start is null.");
                }

                result.Add(new BackTestDrawdownPeriod(currentReturn)
                {
                    Ticker = currentReturn.Ticker,
                    FirstNegativePeriodStart = drawdownFirstPeriodStart.Value,
                    FirstPositivePeriodStart = currentReturn.PeriodStart
                });

                drawdownFirstPeriodStart = null;

                continue;
            }

            // Drawdown started
            if (!wasInDrawdown && inDrawdown)
            {
                drawdownFirstPeriodStart = currentReturn.PeriodStart;

                continue;
            }

            throw new InvalidOperationException();
        }


        return [.. result];
    }

    private static BackTestPeriodReturn[] GetBackTestPeriodReturnDrawdownReturns(BackTestPeriodReturn[] returns)
    {
        var drawdowns = new List<BackTestPeriodReturn>();
        var returnsCount = returns.Length;
        var drawdownStartingBalance = returns[0].StartingBalance;
        var inDrawdown = returns[0].ReturnPercentage < 0;

        for (var i = 0; i < returnsCount; i++)
        {
            var currentReturn = returns[i];

            inDrawdown = currentReturn.EndingBalance < drawdownStartingBalance;

            if (!inDrawdown)
            {
                drawdownStartingBalance = currentReturn.EndingBalance;

                drawdowns.Add(new BackTestPeriodReturn()
                {
                    PeriodStart = currentReturn.PeriodStart,
                    PeriodType = currentReturn.PeriodType,
                    Ticker = currentReturn.Ticker,
                    // TODO StartingBalance is not appropriate, should be null
                    StartingBalance = currentReturn.StartingBalance,
                    ReturnPercentage = 0
                });

                continue;
            }

            drawdowns.Add(new BackTestPeriodReturn()
            {
                PeriodStart = currentReturn.PeriodStart,
                PeriodType = currentReturn.PeriodType,
                Ticker = currentReturn.Ticker,
                // TODO StartingBalance is not appropriate, should be null
                StartingBalance = currentReturn.StartingBalance,
                ReturnPercentage = ((currentReturn.EndingBalance / drawdownStartingBalance - 1) * 100)
            });
        }

        return [.. drawdowns];
    }

    private static BackTestPeriodReturn[] AggregateDecomposedPortfolioBackTest(
        Dictionary<string, BackTestPeriodReturn[]> decomposedBackTest)
    {
        static decimal CalculateReturnPercentage(
            Dictionary<string, BackTestPeriodReturn[]> decomposedBackTest,
            int currentPeriod)
        {
            var rollupStartingBalance = decomposedBackTest.Sum(pair => pair.Value[currentPeriod].StartingBalance);
            var rollupEndingBalance = decomposedBackTest.Sum(pair => pair.Value[currentPeriod].EndingBalance);

            return (rollupEndingBalance / rollupStartingBalance - 1) * 100;
        }

        var firstTickerBackTest = decomposedBackTest.Values.First();

        return firstTickerBackTest
            .Select((_, currentPeriod) => new BackTestPeriodReturn
            {
                PeriodStart = firstTickerBackTest[currentPeriod].PeriodStart,
                PeriodType = firstTickerBackTest[currentPeriod].PeriodType,
                Ticker = null!,
                StartingBalance = decomposedBackTest.Sum(pair => pair.Value[currentPeriod].StartingBalance),
                ReturnPercentage = CalculateReturnPercentage(decomposedBackTest, currentPeriod)
            })
            .ToArray();
    }

    private async Task<Dictionary<string, PeriodReturn[]>> GetDateRangedReturns(
        HashSet<string> tickers,
        PeriodType periodType,
        DateTime firstPeriod,
        DateTime lastPeriod,
        bool includeIncompletePeriod)
    {
        var constituentReturnsByTicker = await returnsService.GetReturnsHistory(
            tickers,
            periodType,
            firstPeriod,
            lastPeriod);

        var constituentReturns = constituentReturnsByTicker.Values;

        var firstSharedFirstPeriod = constituentReturns
            .Select(history => history.First().PeriodStart)
            .Append(firstPeriod)
            .Max();

        var lastSharedLastPeriod = constituentReturns
            .Select(history => history.Last().PeriodStart)
            .Append(lastPeriod)
            .Min();

        if (!includeIncompletePeriod)
        {
            switch (periodType)
            {
                case PeriodType.Daily:
                    break;
                case PeriodType.Monthly:
                    lastSharedLastPeriod = lastSharedLastPeriod.AddDays((lastSharedLastPeriod.Day * -1) + 1).AddMonths(-1);
                    break;
                case PeriodType.Yearly:
                    lastSharedLastPeriod = lastSharedLastPeriod.AddDays((lastSharedLastPeriod.DayOfYear * -1) + 1).AddYears(-1);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

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

    private async Task<(Dictionary<string, BackTestPeriodReturn[]>, Dictionary<string, BackTestRebalanceEvent[]>)> GetPortfolioBackTestDecomposed(
        IEnumerable<BackTestAllocation> portfolioConstituents,
        decimal startingBalance,
        PeriodType periodType,
        DateTime firstPeriod,
        DateTime lastPeriod,
        BackTestRebalanceStrategy rebalanceStrategy,
        decimal? rebalanceBandThreshold,
        bool includeIncompleteEndingPeriod)
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
            lastPeriod,
            includeIncompleteEndingPeriod);

        ConfirmAlignment(dateFilteredReturnsByTicker.Values);

        // No overlapping period, empty results

        if (dateFilteredReturnsByTicker.All(returns => returns.Value.Length == 0))
        {
            var emptyBackTestReturns = dateFilteredReturnsByTicker.Keys.ToDictionary(key => key, _ => Enumerable.Empty<BackTestPeriodReturn>().ToArray());
            var emptyRebalanceEvents = dateFilteredReturnsByTicker.Keys.ToDictionary(key => key, _ => Enumerable.Empty<BackTestRebalanceEvent>().ToArray());

            return (emptyBackTestReturns, emptyRebalanceEvents);
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

    private static (Dictionary<string, BackTestPeriodReturn[]>, Dictionary<string, BackTestRebalanceEvent[]>) GetRebalancedPortfolioBacktest(
        Dictionary<string, PeriodReturn[]> periodAlignedReturnsByTicker,
        Dictionary<string, decimal> targetAllocationsByTicker,
        decimal startingBalance,
        BackTestRebalanceStrategy strategy,
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

        static Dictionary<string, decimal> GetEndingAllocationsByTicker(Dictionary<string, List<BackTestPeriodReturn>> backtest)
        {
            var currentBalancesByTicker = backtest.ToDictionary(
                pair => pair.Key,
                pair => pair.Value[^1].EndingBalance
            );

            var currentTotalBalance = currentBalancesByTicker.Values.Sum();

            return currentBalancesByTicker.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value / currentTotalBalance * 100
            );
        }

        static bool IsBandedRebalanceNeeded(
            Dictionary<string, decimal> targetAllocationsByTicker,
            Dictionary<string, decimal> currentAllocationsByTicker,
            BackTestRebalanceStrategy strategy,
            decimal? threshold) => strategy switch
            {
                BackTestRebalanceStrategy.BandsRelative => IsOutsideRelativeBands(
                    targetAllocationsByTicker,
                    currentAllocationsByTicker,
                    threshold ?? throw new ArgumentNullException(nameof(threshold))),
                BackTestRebalanceStrategy.BandsAbsolute => IsOutsideAbsoluteBands(
                    targetAllocationsByTicker,
                    currentAllocationsByTicker,
                    threshold ?? throw new ArgumentNullException(nameof(threshold))),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };

        static bool IsPeriodicRebalanceNeeded(
            DateTime nextPeriodStartDate,
            DateTime lastRebalancedStartDate,
            BackTestRebalanceStrategy strategy)
            => strategy switch
            {
                BackTestRebalanceStrategy.Annually => nextPeriodStartDate >= lastRebalancedStartDate.AddYears(1),
                BackTestRebalanceStrategy.SemiAnnually => nextPeriodStartDate >= lastRebalancedStartDate.AddMonths(6),
                BackTestRebalanceStrategy.Quarterly => nextPeriodStartDate >= lastRebalancedStartDate.AddMonths(3),
                BackTestRebalanceStrategy.Monthly => nextPeriodStartDate >= lastRebalancedStartDate.AddMonths(1),
                BackTestRebalanceStrategy.Weekly => nextPeriodStartDate >= lastRebalancedStartDate.AddDays(7),
                BackTestRebalanceStrategy.Daily => nextPeriodStartDate >= lastRebalancedStartDate.AddDays(1),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };

        var backtest = periodAlignedReturnsByTicker.ToDictionary(pair => pair.Key, _ => new List<BackTestPeriodReturn>());
        var rebalances = periodAlignedReturnsByTicker.ToDictionary(pair => pair.Key, _ => new List<BackTestRebalanceEvent>());
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
                BackTestRebalanceStrategy.None => false,
                BackTestRebalanceStrategy.BandsAbsolute or BackTestRebalanceStrategy.BandsRelative =>
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

                rebalances[ticker].Add(new BackTestRebalanceEvent
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

    private static BackTestPeriodReturn[] GetPeriodReturnsBackTest(
        PeriodReturn[] tickerReturns,
        decimal startingBalance)
    {
        var performanceTicks = new List<BackTestPeriodReturn>();
        var currentBalance = startingBalance;

        foreach (var currentReturnTick in tickerReturns)
        {
            performanceTicks.Add(new(currentReturnTick.Ticker, currentBalance, currentReturnTick));
            currentBalance = performanceTicks[^1].EndingBalance;
        }

        return [.. performanceTicks];
    }
}

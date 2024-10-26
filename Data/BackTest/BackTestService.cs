using Data.Returns;

namespace Data.BackTest;

internal partial class BackTestService(IReturnsService returnsService/*, ILogger<BackTestService> logger*/) : IBackTestService
{
    public async Task<IEnumerable<BackTest>> GetPortfolioBackTests(
        IEnumerable<IEnumerable<BackTestAllocation>> portfolios,
        decimal? startingBalance,
        PeriodType? periodType,
        DateTime? firstPeriod,
        DateTime? lastPeriod,
        BackTestRebalanceStrategy? rebalanceStrategy,
        decimal? rebalanceBandThreshold,
        bool? includeIncompleteEndingPeriod)
    {
        ArgumentNullException.ThrowIfNull(portfolios);

        startingBalance ??= 100;
        periodType ??= PeriodType.Daily;
        firstPeriod ??= DateTime.MinValue;
        lastPeriod ??= DateTime.MaxValue;
        includeIncompleteEndingPeriod ??= true;
        rebalanceStrategy ??= BackTestRebalanceStrategy.None;
        rebalanceBandThreshold ??= 0;

        if (!portfolios.Any())
        {
            throw new ArgumentException("Portfolios cannot be empty.", nameof(portfolios));
        }

        if (portfolios.Any(portfolio => !portfolio.Any()))
        {
            throw new ArgumentException("A portfolio's constituents cannot be empty.", nameof(portfolios));
        }

        if (portfolios.Any(portfolio => portfolio.Sum(constituent => constituent.Percentage) != 100))
        {
            throw new ArgumentException("A portfolio's constituent percentages must add up to 100.", nameof(portfolios));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(startingBalance.Value, 1, nameof(startingBalance));

        if (lastPeriod.HasValue && firstPeriod.HasValue && lastPeriod.Value < firstPeriod.Value)
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

        var portfolioBackTests = await GetPortfolioBackTestsDecomposed(
            portfolios,
            startingBalance.Value,
            periodType.Value,
            firstPeriod.Value,
            lastPeriod.Value,
            rebalanceStrategy.Value,
            rebalanceBandThreshold,
            includeIncompleteEndingPeriod.Value);

        if (!portfolioBackTests.SelectMany(backTest => backTest.ReturnsByTicker.Values).Any())
        {
            return portfolioBackTests.Select(backTest => new BackTest()
            {
                RebalanceStrategy = rebalanceStrategy.Value,
                RebalanceThreshold = rebalanceBandThreshold
            });
        }

        var backTestResult = portfolioBackTests.Select(portfolioBackTest =>
        {
            var returns = portfolioBackTest.ReturnsByTicker;
            var rebalances = portfolioBackTest.RebalancesByTicker;
            var aggregated = AggregateDecomposedPortfolioBackTest(returns);
            var aggregatedDrawdownReturns = GetBackTestPeriodReturnDrawdownReturns(aggregated);
            var aggregatedDrawdownPeriods = GetBackTestPeriodReturnDrawdownPeriods(aggregated);
            var backTest = new BackTest()
            {
                AggregatePerformance = aggregated,
                AggregatePerformanceDrawdownsReturns = aggregatedDrawdownReturns,
                AggregatePerformanceDrawdownPeriods = aggregatedDrawdownPeriods,
                DecomposedPerformanceByTicker = returns,
                RebalancesByTicker = rebalances,
                RebalanceStrategy = rebalanceStrategy.Value,
                RebalanceThreshold = rebalanceBandThreshold
            };

            return backTest;
        });

        return backTestResult;
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

    private async Task<Dictionary<string, PeriodReturn[]>> GetOverlappingReturns(
        HashSet<string> tickers,
        PeriodType periodType,
        DateTime firstPeriod,
        DateTime lastPeriod,
        bool includeIncompletePeriod)
    {
        static void AssertArrayAlignment(IEnumerable<PeriodReturn[]> dateFilteredConstituentReturns)
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

        AssertArrayAlignment(dateFilteredReturnsByTicker.Values);

        return dateFilteredReturnsByTicker;
    }

    private async Task<IEnumerable<BackTestDecomposed>> GetPortfolioBackTestsDecomposed(
        IEnumerable<IEnumerable<BackTestAllocation>> portfolios,
        decimal startingBalance,
        PeriodType periodType,
        DateTime firstPeriod,
        DateTime lastPeriod,
        BackTestRebalanceStrategy rebalanceStrategy,
        decimal? rebalanceBandThreshold,
        bool includeIncompleteEndingPeriod)
    {
        var allTickers = portfolios.SelectMany(portfolio => portfolio.Select(x => x.Ticker)).Distinct()
            ?? throw new InvalidOperationException("No portfolio tickers.");

        var allReturnsByTicker = await GetOverlappingReturns(
            new(allTickers),
            periodType,
            firstPeriod,
            lastPeriod,
            includeIncompleteEndingPeriod);

        if (allReturnsByTicker.Any(pair => pair.Value.Length == 0))
        {
            return portfolios.Select(portfolio => new BackTestDecomposed());
        }

        var firstTickerReturns = allReturnsByTicker.First().Value;

        firstPeriod = firstTickerReturns.First().PeriodStart;
        lastPeriod = firstTickerReturns.Last().PeriodStart;

        var result = new List<BackTestDecomposed>();

        foreach (var portfolioConstituents in portfolios)
        {
            var dedupedPortfolioConstituents = portfolioConstituents
                .GroupBy(alloc => alloc.Ticker)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(alloc => alloc.Percentage)
                );

            // TODO: GET RID OF AWAIT, USE THREAD.WHENALL
            var dateFilteredReturnsByTicker = await GetOverlappingReturns(
                new(dedupedPortfolioConstituents.Keys),
                periodType,
                firstPeriod,
                lastPeriod,
                includeIncompleteEndingPeriod);

            // No overlapping period, empty results

            if (dateFilteredReturnsByTicker.Any(returns => returns.Value.Length == 0))
            {
                var tickers = dateFilteredReturnsByTicker.Keys;

                var emptyBackTestReturns = tickers.ToDictionary(ticker => ticker, _ => Array.Empty<BackTestPeriodReturn>());
                var emptyRebalanceEvents = tickers.ToDictionary(ticker => ticker, _ => Array.Empty<BackTestRebalanceEvent>());

                result.Add(new BackTestDecomposed()
                {
                    ReturnsByTicker = emptyBackTestReturns,
                    RebalancesByTicker = emptyRebalanceEvents
                });

                continue;
            }

            var portfolioBackTest = GetRebalancedPortfolioBacktest(
                dateFilteredReturnsByTicker,
                dedupedPortfolioConstituents,
                startingBalance,
                rebalanceStrategy,
                rebalanceBandThreshold
            );

            result.Add(portfolioBackTest);
        }

        return result;
    }

    private static BackTestDecomposed GetRebalancedPortfolioBacktest(
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

        return new()
        {
            ReturnsByTicker = backtest.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()),
            RebalancesByTicker = rebalances.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray())
        };
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

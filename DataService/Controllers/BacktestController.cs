using Data.Models;
using Data.Repositories;
using DataService.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers
{
    public readonly struct NominalPeriodReturn(string ticker, decimal startingBalance, PeriodReturn periodReturn)
    {
        public string Ticker { get; init; } = ticker ?? periodReturn.SourceTicker;

        public ReturnPeriod ReturnPeriod { get; init; } = periodReturn.ReturnPeriod;

        public DateTime PeriodStart { get; init; } = periodReturn.PeriodStart;

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public decimal ReturnPercentage { get; init; } = periodReturn.ReturnPercentage;

        public decimal StartingBalance { get; init; } = startingBalance;

        public decimal EndingBalance { get { return StartingBalance + BalanceIncrease; } }

        public decimal BalanceIncrease { get { return StartingBalance * (this.ReturnPercentage / 100m); } }
    }

    public class BacktestController(IReturnRepository returnCache, ILogger<BacktestController> logger) : ControllerBase
    {
        private readonly IReturnRepository returnCache = returnCache;
        private readonly ILogger<BacktestController> logger = logger;

        public async Task<Dictionary<string, NominalPeriodReturn[]>> GetPortfolioBacktest(
            Allocation[] portfolioConstituents,
            decimal startingBalance = 100,
            ReturnPeriod granularity = ReturnPeriod.Daily,
            DateTime startDate = default,
            DateTime? endDate = null,
            RebalanceStrategy rebalanceStrategy = RebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null)
        {
            ArgumentNullException.ThrowIfNull(nameof(portfolioConstituents));
            ArgumentOutOfRangeException.ThrowIfZero(portfolioConstituents.Length, nameof(portfolioConstituents));
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

            var dedupedPortfolioConstituents = portfolioConstituents
                .GroupBy(alloc => alloc.Ticker)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(alloc => alloc.Percentage)
                );

            var constituentTickers = dedupedPortfolioConstituents.Keys.ToArray();
            var constituentReturns = await Task.WhenAll(constituentTickers.Select(ticker => returnCache.Get(ticker, granularity, startDate, endDate.Value)));
            var latestStart = constituentReturns.Select(history => history[0].PeriodStart).Append(startDate).Max();
            var earliestEnd = constituentReturns.Select(history => history[^1].PeriodStart).Append(endDate.Value).Min();

            var dateFilteredConstituentReturns = constituentReturns.Select(constituent => constituent.Where(period => period.PeriodStart >= latestStart && period.PeriodStart <= earliestEnd).ToArray());
            var firstConstituentReturns = dateFilteredConstituentReturns.First();

            if (dateFilteredConstituentReturns.Skip(1).Any(d => d.Length != firstConstituentReturns.Length))
            {
                throw new InvalidOperationException("All decomposed series should (must) have the same length.");
            }

            var backtest = new Dictionary<string, NominalPeriodReturn[]>();

            int periodCount = firstConstituentReturns.Length;
            DateTime lastRebalancePeriodStartDate = firstConstituentReturns[0].PeriodStart;

            for (int currentPeriod = 0; currentPeriod < periodCount; currentPeriod++)
            {
                var firstConstituentCurrentPeriodReturnTick = firstConstituentReturns[currentPeriod];
                var currentPeriodStartDate = firstConstituentCurrentPeriodReturnTick.PeriodStart;

                if (!IsRebalanceRequired(currentPeriodStartDate, lastRebalancePeriodStartDate, rebalanceStrategy, rebalanceBandThreshold))
                {

                    continue;
                }

                lastRebalancePeriodStartDate = currentPeriodStartDate;

                throw new NotImplementedException();
            }

            return backtest;

            /*
            var dateFilteredReturnsByTicker = new Dictionary<string, PeriodReturn[]>();

            for (var i = 0; i < constituentReturns.Length; i++)
            {
                dateFilteredReturnsByTicker[constituentTickers[i]] = constituentReturns[i].Where(r => r.PeriodStart >= latestStart && r.PeriodStart <= earliestEnd).ToArray();
            }
            */
        }

        static bool IsRebalanceRequired(DateTime currentDate, DateTime lastRebalanceDate, RebalanceStrategy strategy, decimal? bandThreshold)
            => strategy switch
            {
                RebalanceStrategy.None => false,
                RebalanceStrategy.Yearly => currentDate >= lastRebalanceDate.AddYears(1),
                RebalanceStrategy.Quarterly => currentDate >= lastRebalanceDate.AddMonths(3),
                RebalanceStrategy.Monthly => currentDate >= lastRebalanceDate.AddMonths(1),
                RebalanceStrategy.Daily => currentDate != lastRebalanceDate,
                RebalanceStrategy.BandsRelative => throw new NotImplementedException(),
                RebalanceStrategy.BandsAbsolute => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };

        private async Task<NominalPeriodReturn[]> GetTickerBacktest(
            string ticker,
            decimal startingBalance,
            ReturnPeriod granularity,
            DateTime startDate,
            DateTime endDate)
        {
            var tickerReturns = await returnCache.Get(ticker, granularity, startDate, endDate);
            return GetPeriodReturnsBacktest([.. tickerReturns], startingBalance);
        }

        private static NominalPeriodReturn[] GetPeriodReturnsBacktest(PeriodReturn[] tickerReturns, decimal startingBalance)
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
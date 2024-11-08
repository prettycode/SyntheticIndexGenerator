using Data.Returns;

namespace Data.BackTest;

public interface IBackTestService
{
    Task<IEnumerable<BackTest>> GetPortfolioBackTests(
        IEnumerable<IEnumerable<BackTestAllocation>> portfolios,
        decimal? startingBalance = null,
        PeriodType? periodType = null,
        DateTime? firstPeriod = null,
        DateTime? lastPeriod = null,
        BackTestRebalanceStrategy? rebalanceStrategy = null,
        decimal? rebalanceBandThreshold = null,
        bool? includeIncompleteEndingPeriod = null);
}
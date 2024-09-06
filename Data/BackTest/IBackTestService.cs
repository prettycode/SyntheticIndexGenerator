using Data.Returns;

namespace Data.BackTest;

public interface IBackTestService
{
    Task<IEnumerable<BackTest>> GetPortfolioBackTests(
        IEnumerable<IEnumerable<BackTestAllocation>> portfolios,
        decimal? startingBalance,
        PeriodType? periodType,
        DateTime? firstPeriod,
        DateTime? lastPeriod,
        BackTestRebalanceStrategy? rebalanceStrategy,
        decimal? rebalanceBandThreshold,
        bool? includeIncompleteEndingPeriod);
}
using Data.Models;
using DataService.Models;

namespace Data.Services
{
    public interface IBackTestService
    {
        Task<BackTest> GetPortfolioBackTest(
            IEnumerable<BackTestAllocation> portfolioConstituents,
            decimal startingBalance = 100,
            PeriodType periodType = PeriodType.Daily,
            DateTime firstPeriod = default,
            DateTime? lastPeriod = null,
            BackTestRebalanceStrategy rebalanceStrategy = BackTestRebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null);
    }
}
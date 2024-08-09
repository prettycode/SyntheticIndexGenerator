using Data.Services;
using Data.Models;
using Microsoft.AspNetCore.Mvc;
using DataService.Models;

namespace DataService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackTestController(IBackTestService backTestService, ILogger<BackTestController> logger) : ControllerBase
    {
        [HttpGet]
        public Task<BackTest> GetPortfolioBackTest(
            IEnumerable<BackTestAllocation> portfolioConstituents,
            decimal startingBalance = 100,
            PeriodType periodType = PeriodType.Daily,
            DateTime firstPeriod = default,
            DateTime? lastPeriod = null,
            BackTestRebalanceStrategy rebalanceStrategy = BackTestRebalanceStrategy.None,
            decimal? rebalanceBandThreshold = null)
            => backTestService.GetPortfolioBackTest(
                portfolioConstituents,
                startingBalance,
                periodType,
                firstPeriod,
                lastPeriod,
                rebalanceStrategy,
                rebalanceBandThreshold);
    }
}
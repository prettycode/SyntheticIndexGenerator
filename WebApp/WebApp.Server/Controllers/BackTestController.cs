using Data.BackTest;
using Data.Returns;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Server.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class BackTestController(IBackTestService backTestService, ILogger<BackTestController> logger) : ControllerBase
{
    [HttpPost]
    public Task<IEnumerable<BackTest>> GetPortfolioBackTests(BackTestRequest backTestRequest)
        => backTestService.GetPortfolioBackTests(
            backTestRequest.Portfolios,
            backTestRequest.StartingBalance,
            backTestRequest.PeriodType,
            backTestRequest.FirstPeriod,
            backTestRequest.LastPeriod,
            backTestRequest.RebalanceStrategy,
            backTestRequest.RebalanceBandThreshold,
            backTestRequest.IncludeIncompleteEndingPeriod);

    [HttpPost]
    public async Task<BackTest> GetPortfolioBackTest(
        IEnumerable<BackTestAllocation> portfolio,
        decimal? startingBalance,
        PeriodType? periodType,
        DateTime? firstPeriod = null,
        DateTime? lastPeriod = null,
        BackTestRebalanceStrategy? rebalanceStrategy = null,
        decimal? rebalanceBandThreshold = null,
        bool? includeIncompleteEndingPeriod = null)
    {
        var backTests = await backTestService.GetPortfolioBackTests(
            [portfolio],
            startingBalance,
            periodType,
            firstPeriod,
            lastPeriod,
            rebalanceStrategy,
            rebalanceBandThreshold,
            includeIncompleteEndingPeriod);

        return backTests.First();
    }
}
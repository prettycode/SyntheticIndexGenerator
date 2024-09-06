using Data.BackTest;
using Data.Returns;
using Microsoft.AspNetCore.Mvc;

namespace WebService.Controllers;

public class BackTestRequest
{
    public required IEnumerable<BackTestAllocation> PortfolioConstituents { get; set; }
    public decimal StartingBalance { get; set; }
    public PeriodType PeriodType { get; set; }
    public DateTime FirstPeriod { get; set; }
    public DateTime? LastPeriod { get; set; }
    public BackTestRebalanceStrategy RebalanceStrategy { get; set; }
    public decimal? RebalanceBandThreshold { get; set; }
}

[ApiController]
[Route("api/[controller]/[action]")]
public class BackTestController(IBackTestService backTestService, ILogger<BackTestController> logger) : ControllerBase
{
    [NonAction]
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

    [HttpPost]
    public Task<BackTest> GetPortfolioBackTest([FromBody] BackTestRequest backTestRequest) => GetPortfolioBackTest(
        backTestRequest.PortfolioConstituents,
        backTestRequest.StartingBalance,
        backTestRequest.PeriodType,
        backTestRequest.FirstPeriod,
        backTestRequest.LastPeriod,
        backTestRequest.RebalanceStrategy,
        backTestRequest.RebalanceBandThreshold);

    [HttpPost]
    public async Task<IEnumerable<BackTest>> GetPortfolioBackTests([FromBody] IEnumerable<BackTestRequest> backTestRequests)
    { 
        var backTestTasks = backTestRequests.Select(backTestRequest => GetPortfolioBackTest(backTestRequest));

        return await Task.WhenAll(backTestTasks);
    }
}
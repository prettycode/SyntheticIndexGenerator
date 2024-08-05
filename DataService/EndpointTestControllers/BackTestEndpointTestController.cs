using Data.Models;
using DataService.Controllers;
using DataService.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataService.TestEndpointControllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BackTestEndpointTestController : ControllerBase
    {
        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>>
            GetPortfolioBackTestDecomposed_NoRebalance_SingleConstituent([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 100 }
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest.DecomposedPerformanceByTicker;
        }

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>>
            GetPortfolioBackTestDecomposed_NoRebalance_DuplicateConstituent([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 }
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest.DecomposedPerformanceByTicker;
        }

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>>
            GetPortfolioBackTestDecomposed_NoRebalance_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest.DecomposedPerformanceByTicker;
        }

        [HttpGet]
        public async Task<Dictionary<string, NominalPeriodReturn[]>>
            GetPortfolioBackTestDecomposed_RebalanceMonthly_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                RebalanceStrategy.Monthly);

            return backtest.DecomposedPerformanceByTicker;
        }

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                RebalanceStrategy.Monthly);

            return backtest;
        }
    }
}
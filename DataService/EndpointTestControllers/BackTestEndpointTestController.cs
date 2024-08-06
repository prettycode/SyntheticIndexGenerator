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
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_NoRebalance_SingleConstituent([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#2X", Percentage = 100 }
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest;
        }

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_NoRebalance_DuplicateConstituent([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#2X", Percentage = 50 },
                new() { Ticker = "#2X", Percentage = 50 }
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest;
        }

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_NoRebalance_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest;
        }

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentMonthlyConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
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

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentDailyConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Daily,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                RebalanceStrategy.Monthly);

            return backtest;
        }

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_RebalanceWeekly_MultipleDifferentDailyConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Daily,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                RebalanceStrategy.Weekly);

            return backtest;
        }

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_RebalanceBands_Absolute_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                RebalanceStrategy.BandsAbsolute,
                0.01m);

            return backtest;
        }

        [HttpGet]
        public async Task<PortfolioBackTest>
            GetPortfolioBackTest_RebalanceBands_Relative_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<Allocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                ReturnPeriod.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                RebalanceStrategy.BandsRelative,
                0.01m);

            return backtest;
        }
    }
}
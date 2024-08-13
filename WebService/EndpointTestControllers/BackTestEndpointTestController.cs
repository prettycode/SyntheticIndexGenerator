using Data.BackTest;
using Data.Returns;
using Microsoft.AspNetCore.Mvc;
using WebService.Controllers;

namespace WebService.EndpointTestControllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BackTestEndpointTestController : ControllerBase
    {
        [HttpGet]
        public async Task TestAll([FromServices] BackTestController controller)
        {
            var httpGetMethods = GetType().GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(HttpGetAttribute), false).Length > 0)
                .Where(m => m.ReturnType == typeof(Task<BackTest>))
                .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(BackTestController))
                .Select(m => (Task<BackTest>)m.Invoke(this, [controller])!);

            await Task.WhenAll(httpGetMethods);
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_NoRebalance_SingleConstituent([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#2X", Percentage = 100 }
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest;
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_NoRebalance_DuplicateConstituent([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#2X", Percentage = 50 },
                new() { Ticker = "#2X", Percentage = 50 }
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest;
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_NoRebalance_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1));

            return backtest;
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentMonthlyConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                BackTestRebalanceStrategy.Monthly);

            return backtest;
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentDailyConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Daily,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                BackTestRebalanceStrategy.Monthly);

            return backtest;
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_RebalanceWeekly_MultipleDifferentDailyConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Daily,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                BackTestRebalanceStrategy.Weekly);

            return backtest;
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_RebalanceBands_Absolute_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                BackTestRebalanceStrategy.BandsAbsolute,
                0.01m);

            return backtest;
        }

        [HttpGet]
        public async Task<BackTest>
            GetPortfolioBackTest_RebalanceBands_Relative_MultipleDifferentConstituents([FromServices] BackTestController controller)
        {
            var portfolio = new List<BackTestAllocation>
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var backtest = await controller.GetPortfolioBackTest(
                portfolio,
                100,
                PeriodType.Monthly,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 1),
                BackTestRebalanceStrategy.BandsRelative,
                0.01m);

            return backtest;
        }
    }
}
using Data.Models;
using DataService.Models;

namespace DataService.Controllers
{
    public class PerformanceControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task GetPortfolioPerformance1()
        {

            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 50 }
            };

            var portfolio2 = new List<Allocation>()
            {
                new() { Ticker = "#2X_PER_PERIOD_2023", Percentage = 100 }
            };

            var controller = base.GetController<PerformanceController>();

            var actualOutput1 = await controller.GetPortfolioPerformance(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));
            var actualOutput2 = await controller.GetPortfolioPerformance(portfolio2, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));

            var expected = new List<PerformanceTick>
            {
                new() {
                    StartingBalance = 100m,
                    BalanceIncrease = 100m,
                    PeriodStart = new DateTime(2023, 1, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 200m,
                    BalanceIncrease = 200m,
                    PeriodStart = new DateTime(2023, 2, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 400m,
                    BalanceIncrease = 400m,
                    PeriodStart = new DateTime(2023, 3, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 800m,
                    BalanceIncrease = 800m,
                    PeriodStart = new DateTime(2023, 4, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 1600m,
                    BalanceIncrease = 1600m,
                    PeriodStart = new DateTime(2023, 5, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 3200m,
                    BalanceIncrease = 3200m,
                    PeriodStart = new DateTime(2023, 6, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 6400m,
                    BalanceIncrease = 6400m,
                    PeriodStart = new DateTime(2023, 7, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 12800m,
                    BalanceIncrease = 12800m,
                    PeriodStart = new DateTime(2023, 8, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 25600m,
                    BalanceIncrease = 25600m,
                    PeriodStart = new DateTime(2023, 9, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 51200m,
                    BalanceIncrease = 51200m,
                    PeriodStart = new DateTime(2023, 10, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 102400m,
                    BalanceIncrease = 102400m,
                    PeriodStart = new DateTime(2023, 11, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 204800m,
                    BalanceIncrease = 204800m,
                    PeriodStart = new DateTime(2023, 12, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                }

            };

            Assert.Equal(actualOutput1, expected);
            Assert.Equal(actualOutput2, expected);
        }

        [Fact]
        public async Task GetPortfolioPerformance2()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            var controller = base.GetController<PerformanceController>();

            var actualOutput1 = await controller.GetPortfolioPerformance(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));

            var expected = new List<PerformanceTick>
            {
                new() {
                    StartingBalance = 100m,
                    BalanceIncrease = 100m,
                    PeriodStart = new DateTime(2023, 1, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 200m,
                    BalanceIncrease = 300m,
                    PeriodStart = new DateTime(2023, 2, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 500m,
                    BalanceIncrease = 900m,
                    PeriodStart = new DateTime(2023, 3, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 1400m,
                    BalanceIncrease = 2700m,
                    PeriodStart = new DateTime(2023, 4, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 4100m,
                    BalanceIncrease = 8100m,
                    PeriodStart = new DateTime(2023, 5, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 12200m,
                    BalanceIncrease = 24300m,
                    PeriodStart = new DateTime(2023, 6, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 36500m,
                    BalanceIncrease = 72900m,
                    PeriodStart = new DateTime(2023, 7, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 109400m,
                    BalanceIncrease = 218700m,
                    PeriodStart = new DateTime(2023, 8, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 328100m,
                    BalanceIncrease = 656100m,
                    PeriodStart = new DateTime(2023, 9, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 984200m,
                    BalanceIncrease = 1968300m,
                    PeriodStart = new DateTime(2023, 10, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 2952500m,
                    BalanceIncrease = 5904900m,
                    PeriodStart = new DateTime(2023, 11, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                },
                new() {
                    StartingBalance = 8857400m,
                    BalanceIncrease = 17714700m,
                    PeriodStart = new DateTime(2023, 12, 1),
                    ReturnPeriod = ReturnPeriod.Monthly
                }
            };

            Assert.Equal(actualOutput1, expected);
        }
    }
}

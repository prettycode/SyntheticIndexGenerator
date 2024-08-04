using Data.Models;
using DataService.Models;
using Xunit;
using Data.Extensions;
using Data.Controllers;
using Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers
{
    public class BackTestControllerTests : ControllerTestBase
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

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));
            var actualOutput2 = await controller.GetPortfolioBackTest(portfolio2, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));

            var expected = new Dictionary<string, NominalPeriodReturn[]>
            {
                {
                    "#2X_PER_PERIOD_2023", new NominalPeriodReturn[]
                    {
                        new("#2X_PER_PERIOD_2023", 100m,    new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 200m,    new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 400m,    new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 800m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 1600m,   new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 3200m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 6400m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 12800m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 25600m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 51200m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 102400m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X_PER_PERIOD_2023", 204800m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                    }
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

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1));

            var expected = new Dictionary<string, NominalPeriodReturn[]>()
            {
                ["#1X_PER_PERIOD_2023"] =
                [
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 100m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X_PER_PERIOD_2023"] =
                [
                    new("#2X_PER_PERIOD_2023", 100m,      new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 300m,      new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 900m,      new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 2700m,     new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 8100m,     new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 24300m,    new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 72900m,    new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 218700m,   new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 656100m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 1968300m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 5904900m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X_PER_PERIOD_2023", 17714700m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 200m, SourceTicker = "#2X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            Assert.Equal(actualOutput1, expected);
        }
    }
}

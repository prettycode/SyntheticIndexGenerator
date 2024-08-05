using System.Text.Json;
using Data.Models;
using DataService.Models;

namespace DataService.Controllers
{
    public class BackTestControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task GetPortfolioBackTest_NoRebalance_SingleConstituent1()
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

            Assert.Equivalent(actualOutput1, expected);
            Assert.Equivalent(actualOutput2, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTest_NoRebalance_MultipleDifferentConstituents1()
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
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X_PER_PERIOD_2023"] =
                [
                    new("#3X_PER_PERIOD_2023", 50m,      new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 150m,     new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 450m,     new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 1350m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 4050m,    new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 12150m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 36450m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 109350m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 328050m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 984150m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 2952450m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 8857350m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equivalent(actualOutput1, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTest_Rebalance_Monthly1()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.Monthly);

            var expected = new Dictionary<string, NominalPeriodReturn[]>
            {
                ["#1X_PER_PERIOD_2023"] =
                [
                    new("#1X_PER_PERIOD_2023",    50m, new() { PeriodStart = new(2023,  1, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023",   100m, new() { PeriodStart = new(2023,  2, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023",   200m, new() { PeriodStart = new(2023,  3, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023",   400m, new() { PeriodStart = new(2023,  4, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023",   800m, new() { PeriodStart = new(2023,  5, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023",  1600m, new() { PeriodStart = new(2023,  6, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023",  3200m, new() { PeriodStart = new(2023,  7, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023",  6400m, new() { PeriodStart = new(2023,  8, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 12800m, new() { PeriodStart = new(2023,  9, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 25600m, new() { PeriodStart = new(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 51200m, new() { PeriodStart = new(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X_PER_PERIOD_2023"] =
                [
                    new("#3X_PER_PERIOD_2023",    50m, new() { PeriodStart = new(2023,  1, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023",   100m, new() { PeriodStart = new(2023,  2, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023",   200m, new() { PeriodStart = new(2023,  3, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023",   400m, new() { PeriodStart = new(2023,  4, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023",   800m, new() { PeriodStart = new(2023,  5, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023",  1600m, new() { PeriodStart = new(2023,  6, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023",  3200m, new() { PeriodStart = new(2023,  7, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023",  6400m, new() { PeriodStart = new(2023,  8, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 12800m, new() { PeriodStart = new(2023,  9, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 25600m, new() { PeriodStart = new(2023, 10, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 51200m, new() { PeriodStart = new(2023, 11, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equivalent(actualOutput1, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTest_RebalanceBands_Absolute_SingleConstituent1()
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

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.BandsAbsolute, 0.0000001m);
            var actualOutput2 = await controller.GetPortfolioBackTest(portfolio2, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.BandsAbsolute, 0.0000001m);

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

            Assert.Equivalent(actualOutput1, expected);
            Assert.Equivalent(actualOutput2, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTest_RebalanceBands_Absolute_MultipleDifferentConstituents1()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X_PER_PERIOD_2023", Percentage = 50 },
                new() { Ticker = "#3X_PER_PERIOD_2023", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.BandsAbsolute, 0.0000001m);

            var expected = new Dictionary<string, NominalPeriodReturn[]>()
            {
                ["#1X_PER_PERIOD_2023"] =
                [
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X_PER_PERIOD_2023", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, SourceTicker = "#1X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X_PER_PERIOD_2023"] =
                [
                    new("#3X_PER_PERIOD_2023", 50m,      new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 150m,     new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 450m,     new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 1350m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 4050m,    new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 12150m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 36450m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 109350m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 328050m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 984150m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 2952450m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X_PER_PERIOD_2023", 8857350m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 200m, SourceTicker = "#3X_PER_PERIOD_2023", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equivalent(actualOutput1, expected);
        }
    }
}

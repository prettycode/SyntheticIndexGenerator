using System.Text.Json;
using Data.Models;
using DataService.Models;

namespace DataService.Controllers
{
    public class BackTestControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task GetPortfolioBackTestDecomposed_NoRebalance_SingleConstituent1()
        {

            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#2X", Percentage = 50 },
                new() { Ticker = "#2X", Percentage = 50 }
            };

            var portfolio2 = new List<Allocation>()
            {
                new() { Ticker = "#2X", Percentage = 100 }
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1))).DecomposedPerformanceByTicker;
            var actualOutput2 = (await controller.GetPortfolioBackTest(portfolio2, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1))).DecomposedPerformanceByTicker;

            var expected = new Dictionary<string, NominalPeriodReturn[]>
            {
                {
                    "#2X", new NominalPeriodReturn[]
                    {
                        new("#2X", 100m,    new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 200m,    new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 400m,    new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 800m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 1600m,   new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 3200m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 6400m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 12800m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 25600m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 51200m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 102400m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 204800m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly })
                    }
                }
            };

            Assert.Equal(actualOutput1, expected);
            Assert.Equal(actualOutput2, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTestDecomposed_NoRebalance_MultipleDifferentConstituents_ManyPeriods()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1))).DecomposedPerformanceByTicker;

            var expected = new Dictionary<string, NominalPeriodReturn[]>()
            {
                ["#1X"] =
                [
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X"] =
                [
                    new("#3X", 50m,      new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 150m,     new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 450m,     new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 1350m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 4050m,    new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 12150m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 36450m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 109350m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 328050m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 984150m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 2952450m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 8857350m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equal(actualOutput1, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTestDecomposed_NoRebalance_MultipleDifferentConstituents_OnePeriod()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 1, 1))).DecomposedPerformanceByTicker;

            var expected = new Dictionary<string, NominalPeriodReturn[]>()
            {
                ["#1X"] =
                [
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X"] =
                [
                    new("#3X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equal(actualOutput1, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTestDecomposed_NoRebalance_MultipleDifferentConstituents_TwoPeriods()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 2, 1))).DecomposedPerformanceByTicker;

            var expected = new Dictionary<string, NominalPeriodReturn[]>()
            {
                ["#1X"] =
                [
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X"] =
                [
                    new("#3X", 50m,  new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 150m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equal(actualOutput1, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTestDecomposed_Rebalance_Monthly1()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.Monthly)).DecomposedPerformanceByTicker;

            var expected = new Dictionary<string, NominalPeriodReturn[]>
            {
                ["#1X"] =
                [
                    new("#1X",     50m, new() { PeriodStart = new(2023,  1, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",    100m, new() { PeriodStart = new(2023,  2, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",    200m, new() { PeriodStart = new(2023,  3, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",    400m, new() { PeriodStart = new(2023,  4, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",    800m, new() { PeriodStart = new(2023,  5, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",   1600m, new() { PeriodStart = new(2023,  6, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",   3200m, new() { PeriodStart = new(2023,  7, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",   6400m, new() { PeriodStart = new(2023,  8, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",  12800m, new() { PeriodStart = new(2023,  9, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",  25600m, new() { PeriodStart = new(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X",  51200m, new() { PeriodStart = new(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 102400m, new() { PeriodStart = new(2023, 12, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly })
                ],
                ["#3X"] =
                [
                    new("#3X",     50m, new() { PeriodStart = new(2023,  1, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",    100m, new() { PeriodStart = new(2023,  2, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",    200m, new() { PeriodStart = new(2023,  3, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",    400m, new() { PeriodStart = new(2023,  4, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",    800m, new() { PeriodStart = new(2023,  5, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",   1600m, new() { PeriodStart = new(2023,  6, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",   3200m, new() { PeriodStart = new(2023,  7, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",   6400m, new() { PeriodStart = new(2023,  8, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",  12800m, new() { PeriodStart = new(2023,  9, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",  25600m, new() { PeriodStart = new(2023, 10, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X",  51200m, new() { PeriodStart = new(2023, 11, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#3X", 102400m, new() { PeriodStart = new(2023, 12, 1), ReturnPercentage = 200m, SourceTicker = "#3X", ReturnPeriod = ReturnPeriod.Monthly })
                ]
            };

            var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equal(actualOutput1, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTestDecomposed_RebalanceBands_Absolute_SingleConstituent1()
        {

            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#2X", Percentage = 50 },
                new() { Ticker = "#2X", Percentage = 50 }
            };

            var portfolio2 = new List<Allocation>()
            {
                new() { Ticker = "#2X", Percentage = 100 }
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.BandsAbsolute, 0.0000001m)).DecomposedPerformanceByTicker;
            var actualOutput2 = (await controller.GetPortfolioBackTest(portfolio2, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.BandsAbsolute, 0.0000001m)).DecomposedPerformanceByTicker;

            var expected = new Dictionary<string, NominalPeriodReturn[]>
            {
                {
                    "#2X", new NominalPeriodReturn[]
                    {
                        new("#2X", 100m,    new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 200m,    new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 400m,    new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 800m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 1600m,   new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 3200m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 6400m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 12800m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 25600m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 51200m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 102400m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                        new("#2X", 204800m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly })
                    }
                }
            };

            Assert.Equal(actualOutput1, expected);
            Assert.Equal(actualOutput2, expected);
        }

        [Fact]
        public async Task GetPortfolioBackTestDecomposed_RebalanceBands_Absolute_MultipleDifferentConstituents1()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#2X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var backtest = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.BandsAbsolute, 25));
            var decomposed = backtest.DecomposedPerformanceByTicker;

            var expectedDecomposed = new Dictionary<string, NominalPeriodReturn[]>
            {
                ["#1X"] = new NominalPeriodReturn[]
                {
                    new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly })
                },
                ["#2X"] = new NominalPeriodReturn[]
                {
                    new("#2X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 100m,        new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 250m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 625m,        new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 1562.5m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 3906.25m,    new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 9765.625m,   new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly })
                }
            };

            var expectedRebalances = new Dictionary<string, RebalanceEvent[]>
            {
                ["#1X"] = new RebalanceEvent[]
                {
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 50m,      BalanceAfterRebalance = 125m,        },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 125m,     BalanceAfterRebalance = 312.5m,      },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 312.5m,   BalanceAfterRebalance = 781.25m,     },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 781.25m,  BalanceAfterRebalance = 1953.125m,   },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 1953.125m, BalanceAfterRebalance = 4882.8125m }
                },
                ["#2X"] = new RebalanceEvent[]
                {
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 200m,      BalanceAfterRebalance = 125m,       },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 500m,      BalanceAfterRebalance = 312.5m,     },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 1250m,     BalanceAfterRebalance = 781.25m,    },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 3125m,     BalanceAfterRebalance = 1953.125m,  },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 7812.5m,   BalanceAfterRebalance = 4882.8125m }
                }
            };

            var actualOutput1Json = JsonSerializer.Serialize(decomposed, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expectedDecomposed, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equal(decomposed, expectedDecomposed);
            Assert.Equal(backtest.RebalancesByTicker, expectedRebalances);
        }

        [Fact]
        public async Task GetPortfolioBackTestDecomposed_RebalanceBands_Relative_MultipleDifferentConstituents1()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#2X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var backtest = (await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.BandsRelative, 50));
            var decomposed = backtest.DecomposedPerformanceByTicker;

            var expectedDecomposed = new Dictionary<string, NominalPeriodReturn[]>
            {
                ["#1X"] = new NominalPeriodReturn[]
                {
                    new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, SourceTicker = "#1X", ReturnPeriod = ReturnPeriod.Monthly })
                },
                ["#2X"] = new NominalPeriodReturn[]
                {
                    new("#2X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 100m,        new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 250m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 625m,        new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 1562.5m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 3906.25m,    new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly }),
                    new("#2X", 9765.625m,   new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, SourceTicker = "#2X", ReturnPeriod = ReturnPeriod.Monthly })
                }
            };

            var expectedRebalances = new Dictionary<string, RebalanceEvent[]>
            {
                ["#1X"] = new RebalanceEvent[]
                {
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 50m,      BalanceAfterRebalance = 125m,        },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 125m,     BalanceAfterRebalance = 312.5m,      },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 312.5m,   BalanceAfterRebalance = 781.25m,     },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 781.25m,  BalanceAfterRebalance = 1953.125m,   },
                    new RebalanceEvent { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 1953.125m, BalanceAfterRebalance = 4882.8125m }
                },
                ["#2X"] = new RebalanceEvent[]
                {
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 200m,      BalanceAfterRebalance = 125m,       },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 500m,      BalanceAfterRebalance = 312.5m,     },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 1250m,     BalanceAfterRebalance = 781.25m,    },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 3125m,     BalanceAfterRebalance = 1953.125m,  },
                    new RebalanceEvent { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = ReturnPeriod.Monthly, BalanceBeforeRebalance = 7812.5m,   BalanceAfterRebalance = 4882.8125m }
                }
            };

            var actualOutput1Json = JsonSerializer.Serialize(decomposed, new JsonSerializerOptions() { WriteIndented = true });
            var expectedJson = JsonSerializer.Serialize(expectedDecomposed, new JsonSerializerOptions() { WriteIndented = true });

            Assert.Equal(decomposed, expectedDecomposed);
            Assert.Equal(backtest.RebalancesByTicker, expectedRebalances);
        }

        [Fact]
        public async Task GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentMonthlyConstituents()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.Monthly);

            var rebalanceDates = actualOutput1.RebalancesByTicker.First().Value.Select(rebalance => rebalance.PrecedingCompletedPeriodStart).ToList();
            var expectedRebalanceDates = new List<DateTime>() {
                new DateTime(2023, 1, 1),
                new DateTime(2023, 2, 1),
                new DateTime(2023, 3, 1),
                new DateTime(2023, 4, 1),
                new DateTime(2023, 5, 1),
                new DateTime(2023, 6, 1),
                new DateTime(2023, 7, 1),
                new DateTime(2023, 8, 1),
                new DateTime(2023, 9, 1),
                new DateTime(2023, 10, 1),
                new DateTime(2023, 11, 1)
            };

            Assert.Equal(rebalanceDates, expectedRebalanceDates);
        }

        [Fact]
        public async Task GetPortfolioBackTest_RebalanceWeekly_MultipleDifferentDailyConstituents()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Daily, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.Weekly);

            var rebalanceDates = actualOutput1.RebalancesByTicker.First().Value.Select(rebalance => rebalance.PrecedingCompletedPeriodStart).ToList();
            var expectedRebalanceDates = new List<DateTime>() {
                new DateTime(2023, 1, 6),
                new DateTime(2023, 1, 13),
                new DateTime(2023, 1, 20),
                new DateTime(2023, 1, 27)
            };

            Assert.Equal(rebalanceDates, expectedRebalanceDates);
        }

        [Fact]
        public async Task GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentDailyConstituents()
        {
            var portfolio1 = new List<Allocation>()
            {
                new() { Ticker = "#1X", Percentage = 50 },
                new() { Ticker = "#3X", Percentage = 50 },
            };

            var controller = base.GetController<BackTestController>();

            var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, ReturnPeriod.Daily, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), RebalanceStrategy.Monthly);

            var rebalanceDates = actualOutput1.RebalancesByTicker.First().Value.Select(rebalance => rebalance.PrecedingCompletedPeriodStart).ToList();
            var expectedRebalanceDates = new List<DateTime>() {
                new DateTime(2023, 2, 1)
            };

            Assert.Equal(rebalanceDates, expectedRebalanceDates);
        }
    }
}

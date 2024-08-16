using System.Text.Json;
using Data.BackTest;
using Data.Returns;

namespace WebService.Controllers;

public class BackTestControllerTests : ControllerTestBase
{
    [Fact]
    public async Task HandlesMultipleTickerTypes()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 25 },
            new() { Ticker = "#3X", Percentage = 25 },
            new() { Ticker = "$USLCB", Percentage = 25 },
            new() { Ticker = "$^USLCV", Percentage = 12.5m },
            new() { Ticker = "AVMC", Percentage = 12.5m }
        };

        var controller = base.GetController<BackTestController>();

        // LCB has no daily
        await Assert.ThrowsAsync<KeyNotFoundException>(() => controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Daily));

        // No overlapping period because $LCB ends before AVMC starts
        var foo1 = await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly);
        Assert.Empty(foo1.AggregatePerformance);

        // AVMC has no yearly
        await Assert.ThrowsAsync<KeyNotFoundException>(() => controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Yearly));


        var portfolio2 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 25 },
            new() { Ticker = "#3X", Percentage = 25 },
            new() { Ticker = "$^USLCV", Percentage = 25 },
            new() { Ticker = "VOO", Percentage = 25 }
        };

        var controller2 = base.GetController<BackTestController>();

        var foo2 = await controller.GetPortfolioBackTest(portfolio2, 100, PeriodType.Daily);
        Assert.NotEmpty(foo2.AggregatePerformance);

        var foo3 = await controller.GetPortfolioBackTest(portfolio2, 100, PeriodType.Monthly);
        Assert.NotEmpty(foo3.AggregatePerformance);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Yearly));


        var portfolio3 = new List<BackTestAllocation>()
        {
            new() { Ticker = "$^USLCV", Percentage = 50 },
            new() { Ticker = "VOO", Percentage = 50 }
        };

        var foo4 = await controller.GetPortfolioBackTest(portfolio3, 100, PeriodType.Daily);
        Assert.NotEmpty(foo4.AggregatePerformance);

        var foo5 = await controller.GetPortfolioBackTest(portfolio3, 100, PeriodType.Monthly);
        Assert.NotEmpty(foo5.AggregatePerformance);

        var foo6 = await controller.GetPortfolioBackTest(portfolio3, 100, PeriodType.Yearly);
        Assert.NotEmpty(foo6.AggregatePerformance);

    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_NoRebalance_SingleConstituent1()
    {

        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#2X", Percentage = 50 },
            new() { Ticker = "#2X", Percentage = 50 }
        };

        var portfolio2 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#2X", Percentage = 100 }
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1))).DecomposedPerformanceByTicker;
        var actualOutput2 = (await controller.GetPortfolioBackTest(portfolio2, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1))).DecomposedPerformanceByTicker;

        var expected = new Dictionary<string, BackTestPeriodReturn[]>
        {
            {
                "#2X", new BackTestPeriodReturn[]
                {
                    new("#2X", 100m,    new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 200m,    new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 400m,    new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 800m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 1600m,   new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 3200m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 6400m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 12800m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 25600m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 51200m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 102400m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 204800m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly })
                }
            }
        };

        Assert.Equal(actualOutput1, expected);
        Assert.Equal(actualOutput2, expected);
    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_NoRebalance_MultipleDifferentConstituents_ManyPeriods()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#3X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1))).DecomposedPerformanceByTicker;

        var expected = new Dictionary<string, BackTestPeriodReturn[]>()
        {
            ["#1X"] =
            [
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly })
            ],
            ["#3X"] =
            [
                new("#3X", 50m,      new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 150m,     new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 450m,     new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 1350m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 4050m,    new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 12150m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 36450m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 109350m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 328050m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 984150m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 2952450m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 8857350m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly })
            ]
        };

        var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
        var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

        Assert.Equal(actualOutput1, expected);
    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_NoRebalance_MultipleDifferentConstituents_OnePeriod()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#3X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 1, 1))).DecomposedPerformanceByTicker;

        var expected = new Dictionary<string, BackTestPeriodReturn[]>()
        {
            ["#1X"] =
            [
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly })
            ],
            ["#3X"] =
            [
                new("#3X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly })
            ]
        };

        var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
        var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

        Assert.Equal(actualOutput1, expected);
    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_NoRebalance_MultipleDifferentConstituents_TwoPeriods()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#3X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 2, 1))).DecomposedPerformanceByTicker;

        var expected = new Dictionary<string, BackTestPeriodReturn[]>()
        {
            ["#1X"] =
            [
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly })
            ],
            ["#3X"] =
            [
                new("#3X", 50m,  new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 150m, new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly })
            ]
        };

        var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
        var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

        Assert.Equal(actualOutput1, expected);
    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_Rebalance_Monthly1()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#3X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.Monthly)).DecomposedPerformanceByTicker;

        var expected = new Dictionary<string, BackTestPeriodReturn[]>
        {
            ["#1X"] =
            [
                new("#1X",     50m, new() { PeriodStart = new(2023,  1, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",    100m, new() { PeriodStart = new(2023,  2, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",    200m, new() { PeriodStart = new(2023,  3, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",    400m, new() { PeriodStart = new(2023,  4, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",    800m, new() { PeriodStart = new(2023,  5, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",   1600m, new() { PeriodStart = new(2023,  6, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",   3200m, new() { PeriodStart = new(2023,  7, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",   6400m, new() { PeriodStart = new(2023,  8, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",  12800m, new() { PeriodStart = new(2023,  9, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",  25600m, new() { PeriodStart = new(2023, 10, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X",  51200m, new() { PeriodStart = new(2023, 11, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 102400m, new() { PeriodStart = new(2023, 12, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly })
            ],
            ["#3X"] =
            [
                new("#3X",     50m, new() { PeriodStart = new(2023,  1, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",    100m, new() { PeriodStart = new(2023,  2, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",    200m, new() { PeriodStart = new(2023,  3, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",    400m, new() { PeriodStart = new(2023,  4, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",    800m, new() { PeriodStart = new(2023,  5, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",   1600m, new() { PeriodStart = new(2023,  6, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",   3200m, new() { PeriodStart = new(2023,  7, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",   6400m, new() { PeriodStart = new(2023,  8, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",  12800m, new() { PeriodStart = new(2023,  9, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",  25600m, new() { PeriodStart = new(2023, 10, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X",  51200m, new() { PeriodStart = new(2023, 11, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly }),
                new("#3X", 102400m, new() { PeriodStart = new(2023, 12, 1), ReturnPercentage = 200m, Ticker = "#3X", PeriodType = PeriodType.Monthly })
            ]
        };

        var actualOutput1Json = JsonSerializer.Serialize(actualOutput1, new JsonSerializerOptions() { WriteIndented = true });
        var expectedJson = JsonSerializer.Serialize(expected, new JsonSerializerOptions() { WriteIndented = true });

        Assert.Equal(actualOutput1, expected);
    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_RebalanceBands_Absolute_SingleConstituent1()
    {

        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#2X", Percentage = 50 },
            new() { Ticker = "#2X", Percentage = 50 }
        };

        var portfolio2 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#2X", Percentage = 100 }
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.BandsAbsolute, 0.0000001m)).DecomposedPerformanceByTicker;
        var actualOutput2 = (await controller.GetPortfolioBackTest(portfolio2, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.BandsAbsolute, 0.0000001m)).DecomposedPerformanceByTicker;

        var expected = new Dictionary<string, BackTestPeriodReturn[]>
        {
            {
                "#2X", new BackTestPeriodReturn[]
                {
                    new("#2X", 100m,    new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 200m,    new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 400m,    new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 800m,    new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 1600m,   new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 3200m,   new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 6400m,   new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 12800m,  new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 25600m,  new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1),  ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 51200m,  new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 102400m, new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                    new("#2X", 204800m, new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly })
                }
            }
        };

        Assert.Equal(actualOutput1, expected);
        Assert.Equal(actualOutput2, expected);
    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_RebalanceBands_Absolute_MultipleDifferentConstituents1()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#2X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var backtest = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.BandsAbsolute, 25));
        var decomposed = backtest.DecomposedPerformanceByTicker;

        var expectedDecomposed = new Dictionary<string, BackTestPeriodReturn[]>
        {
            ["#1X"] =
            [
                new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly })
            ],
            ["#2X"] =
            [
                new("#2X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 100m,        new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 250m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 625m,        new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 1562.5m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 3906.25m,    new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 9765.625m,   new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly })
            ]
        };

        var expectedRebalances = new Dictionary<string, BackTestRebalanceEvent[]>
        {
            ["#1X"] =
            [
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 50m,      BalanceAfterRebalance = 125m,        },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 125m,     BalanceAfterRebalance = 312.5m,      },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 312.5m,   BalanceAfterRebalance = 781.25m,     },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 781.25m,  BalanceAfterRebalance = 1953.125m,   },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 1953.125m, BalanceAfterRebalance = 4882.8125m }
            ],
            ["#2X"] =
            [
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 200m,      BalanceAfterRebalance = 125m,       },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 500m,      BalanceAfterRebalance = 312.5m,     },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 1250m,     BalanceAfterRebalance = 781.25m,    },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 3125m,     BalanceAfterRebalance = 1953.125m,  },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 7812.5m,   BalanceAfterRebalance = 4882.8125m }
            ]
        };

        var actualOutput1Json = JsonSerializer.Serialize(decomposed, new JsonSerializerOptions() { WriteIndented = true });
        var expectedJson = JsonSerializer.Serialize(expectedDecomposed, new JsonSerializerOptions() { WriteIndented = true });

        Assert.Equal(decomposed, expectedDecomposed);
        Assert.Equal(backtest.RebalancesByTicker, expectedRebalances);
    }

    [Fact]
    public async Task GetPortfolioBackTestDecomposed_RebalanceBands_Relative_MultipleDifferentConstituents1()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#2X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var backtest = (await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.BandsRelative, 50));
        var decomposed = backtest.DecomposedPerformanceByTicker;

        var expectedDecomposed = new Dictionary<string, BackTestPeriodReturn[]>
        {
            ["#1X"] =
            [
                new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly }),
                new("#1X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 0m, Ticker = "#1X", PeriodType = PeriodType.Monthly })
            ],
            ["#2X"] =
            [
                new("#2X", 50m,         new PeriodReturn { PeriodStart = new DateTime(2023, 1, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 100m,        new PeriodReturn { PeriodStart = new DateTime(2023, 2, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 125m,        new PeriodReturn { PeriodStart = new DateTime(2023, 3, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 250m,        new PeriodReturn { PeriodStart = new DateTime(2023, 4, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 312.5m,      new PeriodReturn { PeriodStart = new DateTime(2023, 5, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 625m,        new PeriodReturn { PeriodStart = new DateTime(2023, 6, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 781.25m,     new PeriodReturn { PeriodStart = new DateTime(2023, 7, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 1562.5m,     new PeriodReturn { PeriodStart = new DateTime(2023, 8, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 1953.125m,   new PeriodReturn { PeriodStart = new DateTime(2023, 9, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 3906.25m,    new PeriodReturn { PeriodStart = new DateTime(2023, 10, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 4882.8125m,  new PeriodReturn { PeriodStart = new DateTime(2023, 11, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly }),
                new("#2X", 9765.625m,   new PeriodReturn { PeriodStart = new DateTime(2023, 12, 1), ReturnPercentage = 100m, Ticker = "#2X", PeriodType = PeriodType.Monthly })
            ]
        };

        var expectedRebalances = new Dictionary<string, BackTestRebalanceEvent[]>
        {
            ["#1X"] =
            [
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 50m,      BalanceAfterRebalance = 125m,        },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 125m,     BalanceAfterRebalance = 312.5m,      },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 312.5m,   BalanceAfterRebalance = 781.25m,     },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 781.25m,  BalanceAfterRebalance = 1953.125m,   },
                new() { Ticker = "#1X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 1953.125m, BalanceAfterRebalance = 4882.8125m }
            ],
            ["#2X"] =
            [
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 2, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 200m,      BalanceAfterRebalance = 125m,       },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 4, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 500m,      BalanceAfterRebalance = 312.5m,     },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 6, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 1250m,     BalanceAfterRebalance = 781.25m,    },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 8, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 3125m,     BalanceAfterRebalance = 1953.125m,  },
                new() { Ticker = "#2X", PrecedingCompletedPeriodStart = new DateTime(2023, 10, 1), PrecedingCompletedPeriodType = PeriodType.Monthly, BalanceBeforeRebalance = 7812.5m,   BalanceAfterRebalance = 4882.8125m }
            ]
        };

        var actualOutput1Json = JsonSerializer.Serialize(decomposed, new JsonSerializerOptions() { WriteIndented = true });
        var expectedJson = JsonSerializer.Serialize(expectedDecomposed, new JsonSerializerOptions() { WriteIndented = true });

        Assert.Equal(decomposed, expectedDecomposed);
        Assert.Equal(backtest.RebalancesByTicker, expectedRebalances);
    }

    [Fact]
    public async Task GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentMonthlyConstituents()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#3X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Monthly, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.Monthly);

        var rebalanceDates = actualOutput1.RebalancesByTicker.First().Value.Select(rebalance => rebalance.PrecedingCompletedPeriodStart).ToList();
        var expectedRebalanceDates = new List<DateTime>() {
            new(2023, 1, 1),
            new(2023, 2, 1),
            new(2023, 3, 1),
            new(2023, 4, 1),
            new(2023, 5, 1),
            new(2023, 6, 1),
            new(2023, 7, 1),
            new(2023, 8, 1),
            new(2023, 9, 1),
            new(2023, 10, 1),
            new(2023, 11, 1)
        };

        Assert.Equal(rebalanceDates, expectedRebalanceDates);
    }

    [Fact]
    public async Task GetPortfolioBackTest_RebalanceWeekly_MultipleDifferentDailyConstituents()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#3X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Daily, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.Weekly);

        var actualRebalanceDates = actualOutput1.RebalancesByTicker.First().Value.Select(rebalance => rebalance.PrecedingCompletedPeriodStart).ToList();
        var expectedRebalanceDates = new List<DateTime>() {
            new(2023, 1, 9),
            new(2023, 1, 13),
            new(2023, 1, 23),
            new(2023, 1, 30),
            new(2023, 2, 6)
        };

        Assert.Equal(expectedRebalanceDates, actualRebalanceDates);
    }

    [Fact]
    public async Task GetPortfolioBackTest_RebalanceMonthly_MultipleDifferentDailyConstituents()
    {
        var portfolio1 = new List<BackTestAllocation>()
        {
            new() { Ticker = "#1X", Percentage = 50 },
            new() { Ticker = "#3X", Percentage = 50 },
        };

        var controller = base.GetController<BackTestController>();

        var actualOutput1 = await controller.GetPortfolioBackTest(portfolio1, 100, PeriodType.Daily, new DateTime(2023, 1, 1), new DateTime(2023, 12, 1), BackTestRebalanceStrategy.Monthly);

        var actualRebalanceDates = actualOutput1.RebalancesByTicker.First().Value.Select(rebalance => rebalance.PrecedingCompletedPeriodStart).ToList();
        var expectedRebalanceDates = new List<DateTime>() {
            new(2023, 2, 2)
        };

        Assert.Equal(expectedRebalanceDates, actualRebalanceDates);
    }
}

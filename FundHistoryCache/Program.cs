using FundHistoryCache.Controllers;
using FundHistoryCache.Models;
using FundHistoryCache.Repositories;
using Timer = FundHistoryCache.Utils.Timer;

var quotesPath = "../../../data/quotes/";
var syntheticReturnsFilePath = "../../../source/Stock-Index-Data-20220923-Monthly.csv";
var saveSyntheticReturnsPath = "../../../data/returns/";

var quoteRepository = new QuoteRepository(quotesPath);
var returnsRepository = new ReturnsRepository(saveSyntheticReturnsPath, syntheticReturnsFilePath);
var quoteTickersNeeded = IndicesController.GetBackfillTickers();

await Timer.Exec("Refresh quotes", QuotesController.RefreshQuotes(quoteRepository, quoteTickersNeeded));
await Timer.Exec("Refresh returns", ReturnsController.RefreshReturns(quoteRepository, returnsRepository));
await Timer.Exec("Refresh indices", IndicesController.RefreshIndices(returnsRepository));

GetPerformance(returnsRepository, "AVUV").Result.ForEach(tick => Console.WriteLine($"AVUV: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));

static async Task<List<PerformanceTick>> GetPerformance(
    ReturnsRepository returnsCache,
    string ticker,
    decimal startingBalance = 100,
    ReturnPeriod granularity = ReturnPeriod.Daily)
{
    var tickerReturns = await returnsCache.Get(ticker, granularity)
        ?? throw new ArgumentException($"No returns found in `{nameof(returnsCache)}`", nameof(ticker));

    var performanceTicks = new List<PerformanceTick>();

    foreach (var currentReturnTick in tickerReturns)
    {
        performanceTicks.Add(new PerformanceTick(currentReturnTick, startingBalance));
        startingBalance = performanceTicks.Last().EndingBalance;
    }

    return performanceTicks;
}

class PerformanceTick
{
    public PeriodReturn Period { get; set; }

    public decimal StartingBalance { get; set; }

    public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

    public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }

    public PerformanceTick(PeriodReturn period, decimal startingBalance)
    {
        this.Period = period;
        this.StartingBalance = startingBalance;
    }
}

using FundHistoryCache.Controllers;
using FundHistoryCache.Models;
using FundHistoryCache.Repositories;
using Timer = FundHistoryCache.Utils.Timer;

var quotesPath = "../../../data/quotes/";
var syntheticReturnsFilePath = "../../../source/Stock-Index-Data-20220923-Monthly.csv";
var saveSyntheticReturnsPath = "../../../data/returns/";

var quoteRepository = new QuoteRepository(quotesPath);
var returnsRepository = new ReturnRepository(saveSyntheticReturnsPath, syntheticReturnsFilePath);
var quoteTickersNeeded = IndexController.GetBackfillTickers();

await Timer.Exec("Refresh quotes", QuoteController.RefreshQuotes(quoteRepository, quoteTickersNeeded));
await Timer.Exec("Refresh returns", ReturnController.RefreshReturns(quoteRepository, returnsRepository));
await Timer.Exec("Refresh indices", IndexController.RefreshIndices(returnsRepository));

GetPerformance(returnsRepository, quoteRepository, "AVUV")
    .Result
    .ForEach(tick => Console.WriteLine($"AVUV: {tick.Period.PeriodStart:yyyy-MM-dd} {tick.EndingBalance:C} ({tick.BalanceIncrease:N2}%)"));

static async Task<List<PerformanceTick>> GetPerformance(
    ReturnRepository returnsCache,
    QuoteRepository quotesCache,
    string ticker,
    decimal startingBalance = 100,
    ReturnPeriod granularity = ReturnPeriod.Daily)
{
    if (!returnsCache.Has(ticker, granularity))
    {
        if (!quotesCache.Has(ticker))
        {
            var successful = await QuoteController.RefreshQuote(quotesCache, ticker);

            if (!successful)
            {
                throw new InvalidOperationException();
            }
        }

        await ReturnController.RefreshReturn(quotesCache, returnsCache, ticker);
    }

    var tickerReturns = await returnsCache.Get(ticker, granularity)
        ?? throw new ArgumentException($"No returns found in `{nameof(returnsCache)}`", nameof(ticker));

    var performanceTicks = new List<PerformanceTick>();

    foreach (var currentReturnTick in tickerReturns)
    {
        performanceTicks.Add(new(currentReturnTick, startingBalance));
        startingBalance = performanceTicks[^1].EndingBalance;
    }

    return performanceTicks;
}

class PerformanceTick(PeriodReturn period, decimal startingBalance)
{
    public PeriodReturn Period { get; set; } = period;

    public decimal StartingBalance { get; set; } = startingBalance;

    public decimal EndingBalance { get { return this.StartingBalance + this.BalanceIncrease; } }

    public decimal BalanceIncrease { get { return this.StartingBalance * (this.Period.ReturnPercentage / 100m); } }
}

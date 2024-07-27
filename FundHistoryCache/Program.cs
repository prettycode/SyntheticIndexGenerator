using FundHistoryCache.Controllers;
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
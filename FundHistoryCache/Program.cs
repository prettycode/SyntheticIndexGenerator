var quotesPath = "../../../data/quotes/";
var syntheticReturnsFilePath = "../../../source/Stock-Index-Data-20220923-Monthly.csv";
var saveSyntheticReturnsPath = "../../../data/returns/";

var quoteRepository = new QuoteRepository(quotesPath);
var quoteTickersNeeded = IndicesController.GetBackfillTickers();

await QuotesController.RefreshQuotes(quoteRepository, quoteTickersNeeded);
await ReturnsController.RefreshReturns(quoteRepository, syntheticReturnsFilePath, saveSyntheticReturnsPath);
await IndicesController.RefreshIndices(saveSyntheticReturnsPath);
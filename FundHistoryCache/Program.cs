var quotesPath = "../../../data/quotes/";
var syntheticReturnsFilePath = "../../../source/Stock-Index-Data-20220923-Monthly.csv";
var saveSyntheticReturnsPath = "../../../data/returns/";
var quoteTickers = IndicesController.GetBackfillTickers();

var fundRepository = new QuoteRepository(quotesPath);

await QuotesController.RefreshQuotes(fundRepository, quoteTickers);
await ReturnsController.RefreshReturns(fundRepository, syntheticReturnsFilePath, saveSyntheticReturnsPath);
await IndicesController.RefreshIndices(saveSyntheticReturnsPath);
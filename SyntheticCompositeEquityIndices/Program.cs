var rootPath = "../../../..";
var fundRepositorySourcePath = Path.Combine(rootPath, "./FundHistoryCache/data/");
var syntheticUsSourcePath = Path.Combine(rootPath, "./SyntheticUsEquityIndices/source/Stock-Index-Data-20220923-Monthly.csv");
var syntheticUsSavePath = Path.Combine(rootPath, "./SyntheticUsEquityIndices/data/monthly/");
var fundHistoryReturnsSavePath = Path.Combine(rootPath, "./FundHistoryReturns/data/");

var fundRepository = new FundHistoryRepository(fundRepositorySourcePath);

await Task.WhenAll(
    SyntheticUsEquityIndicesController.SaveParsedReturnsToReturnsHistory(syntheticUsSourcePath, syntheticUsSavePath),
    FundHistoryCacheController.RefreshFundHistoryCache(fundRepository)
);

await FundHistoryReturnsController.WriteAllFundHistoryReturns(fundRepository, fundHistoryReturnsSavePath);

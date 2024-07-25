var rootPath = "../../../..";
var fundRepository = new FundHistoryRepository($"{rootPath}/FundHistoryCache/data/");

await Task.WhenAll(
    SyntheticUsEquityIndicesController.SaveParsedReturnsToReturnsHistory(
        $"{rootPath}/SyntheticUsEquityIndices/source/Stock-Index-Data-20220923-Monthly.csv",
        $"{rootPath}/SyntheticUsEquityIndices/data/monthly/"
    ),
    FundHistoryCacheController.RefreshFundHistoryCache(fundRepository)
);

await FundHistoryReturnsController.WriteAllFundHistoryReturns(fundRepository, $"{rootPath}/FundHistoryReturns/data/");

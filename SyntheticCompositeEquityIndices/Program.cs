throw new NotImplementedException();

var fundRepository = new FundHistoryRepository("../../../../FundHistoryCache/data/");

await Task.WhenAll(
    SyntheticUsEquityIndicesController.SaveParsedReturnsToReturnsHistory(
        "../../../../SyntheticUsEquityIndices/source/Stock-Index-Data-20220923-Monthly.csv", 
        "../../../../SyntheticUsEquityIndices/data/monthly/"
    ),
    FundHistoryCacheController.RefreshFundHistoryCache(fundRepository)
);

await FundHistoryReturnsController.WriteAllFundHistoryReturns(fundRepository, "../../../../SyntheticUsEquityIndices/data/");

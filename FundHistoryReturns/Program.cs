var fundRepositoryDataPath = "../../../../FundHistoryCache/data/";
var fundRepository = new FundHistoryRepository(fundRepositoryDataPath);
var historyReturnPath = "../../../data/";

await FundHistoryReturnsController.WriteAllFundHistoryReturns(fundRepository, historyReturnPath);

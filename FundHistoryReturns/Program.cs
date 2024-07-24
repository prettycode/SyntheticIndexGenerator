var cache = new FundHistoryRepository("../../../../FundHistoryCache/data");
var tickers = cache.GetCacheKeys();

await TimerUtility.TimeExecution("Write fund returns", async () =>
{
    await Task.WhenAll(tickers.Select(async ticker =>
    {
        var history = await cache.Get(ticker);
        var priceHistory = history!.Prices.ToList();

        await Task.WhenAll(
            WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Daily),
            WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Monthly),
            WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Yearly)
        );
    }));
});

static async Task WriteFundHistoryReturns(string ticker, List<PriceRecord> history, TimePeriod period)
{
    List<KeyValuePair<DateTime, decimal>> returns = period switch
    {
        TimePeriod.Daily => FundHistoryPeriodReturns.GetDailyReturns(history),
        TimePeriod.Monthly => FundHistoryPeriodReturns.GetMonthReturns(history),
        TimePeriod.Yearly => FundHistoryPeriodReturns.GetYearlyReturns(history),
        _ => throw new NotImplementedException()
    };

    string csvFilePath = $"../../../data/{period.ToString().ToLowerInvariant()}/{ticker}.csv";
    var csvFileLines = returns.Select(r => $"{r.Key:yyyy-MM-dd},{r.Value}");

    await File.WriteAllLinesAsync(csvFilePath, csvFileLines);
}

enum TimePeriod
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}
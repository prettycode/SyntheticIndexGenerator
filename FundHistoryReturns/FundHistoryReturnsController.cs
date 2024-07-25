public static class FundHistoryReturnsController
{
    public static async Task WriteAllFundHistoryReturns(FundHistoryRepository cache, string savePath)
    {
        var tickers = cache.GetCacheKeys();

        await Task.WhenAll(tickers.Select(async ticker =>
        {
            var history = await cache.Get(ticker);
            var priceHistory = history!.Prices.ToList();

            await Task.WhenAll(
                WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Daily, savePath),
                WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Monthly, savePath),
                WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Yearly, savePath)
            );
        }));
    }

    public static async Task WriteFundHistoryReturns(string ticker, List<PriceRecord> history, TimePeriod period, string savePath)
    {
        List<KeyValuePair<DateTime, decimal>> returns = period switch
        {
            TimePeriod.Daily => FundHistoryPeriodReturns.GetDailyReturns(history),
            TimePeriod.Monthly => FundHistoryPeriodReturns.GetMonthReturns(history),
            TimePeriod.Yearly => FundHistoryPeriodReturns.GetYearlyReturns(history),
            _ => throw new NotImplementedException()
        };

        string csvFilePath = Path.Combine(savePath, $"{period.ToString().ToLowerInvariant()}/{ticker}.csv");
        var csvFileLines = returns.Select(r => $"{r.Key:yyyy-MM-dd},{r.Value}");

        await File.WriteAllLinesAsync(csvFilePath, csvFileLines);
    }
}
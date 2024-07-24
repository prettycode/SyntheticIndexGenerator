var cache = new FundHistoryRepository("../../../../FundHistoryCache/data");

foreach(var ticker in cache.GetCacheKeys())
{
    var fundHistory = await cache.Get(ticker);
    var fundPriceHistory = fundHistory!.Prices.ToList();

    await WriteFundHistoryReturns(ticker, fundPriceHistory, TimePeriod.Daily);
    await WriteFundHistoryReturns(ticker, fundPriceHistory, TimePeriod.Monthly);
    await WriteFundHistoryReturns(ticker, fundPriceHistory, TimePeriod.Yearly);
}

static async Task WriteFundHistoryReturns(string ticker, List<PriceRecord> history, TimePeriod period)
{
    static string getFilePath(TimePeriod period, string ticker) => $"../../../data/{period.ToString().ToLowerInvariant()}/{ticker}.csv";
    List<KeyValuePair<DateTime, decimal>> returns = period switch
    {
        TimePeriod.Daily => FundHistoryPeriodReturns.GetDailyReturns(history),
        TimePeriod.Monthly => FundHistoryPeriodReturns.GetMonthReturns(history),
        TimePeriod.Yearly => FundHistoryPeriodReturns.GetYearlyReturns(history),
        _ => throw new NotImplementedException()
    };

    await File.WriteAllLinesAsync(getFilePath(period, ticker), returns.Select(r => $"{DateOnly.FromDateTime(r.Key)},{r.Value}"));
}

enum TimePeriod
{
    Daily,
    Monthly,
    Yearly
}
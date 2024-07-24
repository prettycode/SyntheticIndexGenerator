using YahooFinanceApi;

public static class FundHistoryDownloader
{

    public static Task<FundHistory> GetMissingHistory(FundHistory history, out DateTime start, out DateTime end)
    {
        if (history == null)
        {
            throw new ArgumentNullException(nameof(history));
        }

        var lastDate = history.Prices[history.Prices.Count - 1].DateTime.Date;

        start = lastDate.AddDays(1);
        end = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);

        if (!(start < end))
        {
            return Task.FromResult<FundHistory>(null!);
        }

        return FundHistoryDownloader.GetHistoryRange(history.Ticker, start, end);
    }

    public static Task<FundHistory> GetHistoryAll(string ticker)
    {
        return FundHistoryDownloader.GetHistoryRange(ticker, new DateTime(1900, 1, 1), DateTime.UtcNow);
    }

    private static async Task<FundHistory> GetHistoryRange(string ticker, DateTime start, DateTime end)
    {
        if (start == end)
        {
            return null!;
        }

        FundHistory fundHistory = new(ticker);
        static async Task<T> throttle<T>(Func<Task<T>> operation)
        {
            await Task.Delay(1000);
            return await operation();
        }

        fundHistory.Dividends = (await throttle(() => Yahoo.GetDividendsAsync(ticker, start, end))).Select(divTick => new DividendRecord(divTick)).ToList();
        fundHistory.Prices = (await throttle(() => Yahoo.GetHistoricalAsync(ticker, start, end))).Select(candle => new PriceRecord(candle)).ToList();
        fundHistory.Splits = (await throttle(() => Yahoo.GetSplitsAsync(ticker, start, end))).Select(splitTick => new SplitRecord(splitTick)).ToList();

        if (fundHistory.Prices[fundHistory.Prices.Count - 1].Open == 0)
        {
            fundHistory.Prices = fundHistory.Prices.Take(fundHistory.Prices.Count - 1).ToList();
        }

        return fundHistory;
    }
}
using YahooFinanceApi;

public static class QuotesController
{
    public static async Task RefreshQuotes(QuoteRepository cache, HashSet<string> tickers)
    {
        foreach (var ticker in tickers)
        {
            Console.WriteLine();

            var fundHistory = await cache.Get(ticker);

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history found in cache.");
            }
            else
            {
                Console.WriteLine($"{ticker}: {fundHistory.Prices.Count} record(s) in cache, {fundHistory.Prices[0].DateTime:yyyy-MM-dd} to {fundHistory.Prices[fundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}.");
            }

            Quote? missingFundHistory;

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: Download entire history.");

                missingFundHistory = await QuotesController.GetHistoryAll(ticker);
            }
            else
            {
                missingFundHistory = await QuotesController.GetMissingHistory(fundHistory, out DateTime missingStart, out DateTime missingEnd);

                if (missingFundHistory != null)
                {
                    Console.WriteLine($"{ticker}: Missing history identified as {missingStart:yyyy-MM-dd} to {missingEnd:yyyy-MM-dd}");
                }
            }

            if (missingFundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history missing.");
                continue;
            }

            Console.WriteLine($"{ticker}: Save {missingFundHistory.Prices.Count} new record(s), {missingFundHistory.Prices[0].DateTime:yyyy-MM-dd} to {missingFundHistory.Prices[missingFundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}.");

            await cache.Put(missingFundHistory);
        }
    }


    public static Task<Quote> GetMissingHistory(Quote history, out DateTime start, out DateTime end)
    {
        ArgumentNullException.ThrowIfNull(history);

        var lastDate = history.Prices[history.Prices.Count - 1].DateTime.Date;

        start = lastDate.AddDays(1);
        end = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);

        if (!(start < end))
        {
            return Task.FromResult<Quote>(null!);
        }

        return QuotesController.GetHistoryRange(history.Ticker, start, end);
    }

    public static Task<Quote> GetHistoryAll(string ticker)
    {
        return QuotesController.GetHistoryRange(ticker, new DateTime(1900, 1, 1), DateTime.UtcNow);
    }

    private static async Task<Quote> GetHistoryRange(string ticker, DateTime start, DateTime end)
    {
        if (start == end)
        {
            return null!;
        }

        Quote fundHistory = new(ticker);
        static async Task<T> throttle<T>(Func<Task<T>> operation)
        {
            await Task.Delay(1000);
            return await operation();
        }

        fundHistory.Dividends = (await throttle(() => Yahoo.GetDividendsAsync(ticker, start, end))).Select(divTick => new QuoteDividendRecord(divTick)).ToList();
        fundHistory.Prices = (await throttle(() => Yahoo.GetHistoricalAsync(ticker, start, end))).Select(candle => new QuotePriceRecord(candle)).ToList();
        fundHistory.Splits = (await throttle(() => Yahoo.GetSplitsAsync(ticker, start, end))).Select(splitTick => new QuoteSplitRecord(splitTick)).ToList();

        if (fundHistory.Prices[fundHistory.Prices.Count - 1].Open == 0)
        {
            fundHistory.Prices = fundHistory.Prices.Take(fundHistory.Prices.Count - 1).ToList();
        }

        return fundHistory;
    }
}
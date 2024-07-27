using YahooFinanceApi;

public static class QuotesController
{
    public static async Task RefreshQuotes(QuoteRepository quotesCache, HashSet<string> tickers)
    {
        ArgumentNullException.ThrowIfNull(quotesCache);
        ArgumentNullException.ThrowIfNull(tickers);

        foreach (var ticker in tickers)
        {
            Console.WriteLine();

            var fundHistory = await quotesCache.Get(ticker);

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history found in cache.");
            }
            else
            {
                Console.WriteLine($"{ticker}: {fundHistory.Prices.Count} record(s) in cache, {fundHistory.Prices[0].DateTime:yyyy-MM-dd} to {fundHistory.Prices[^1].DateTime:yyyy-MM-dd}.");
            }

            Quote? missingFundHistory;

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: Download entire history.");

                try
                {
                    missingFundHistory = await QuotesController.GetHistoryAll(ticker);
                }
                catch (Exception ex)
                {
                    // API will sometimes return 404 if ticker is being updated on their end
                    Console.WriteLine($"{ticker}: ERROR DOWNLOAD FAILED: {ex.Message}");
                    continue;
                }
            }
            else
            {
                try
                {
                    missingFundHistory = await QuotesController.GetMissingHistory(fundHistory, out DateTime missingStart, out DateTime missingEnd);

                    if (missingFundHistory != null)
                    {
                        Console.WriteLine($"{ticker}: Missing history identified as {missingStart:yyyy-MM-dd} to {missingEnd:yyyy-MM-dd}");
                    }
                }
                catch (Exception ex)
                {
                    // API will sometimes return 404 if ticker is being updated on their end
                    Console.WriteLine($"{ticker}: ERROR DOWNLOAD FAILED: {ex.Message}");
                    continue;
                }
            }

            if (missingFundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history missing.");
                continue;
            }

            Console.WriteLine($"{ticker}: Save {missingFundHistory.Prices.Count} new record(s), {missingFundHistory.Prices[0].DateTime:yyyy-MM-dd} to {missingFundHistory.Prices[^1].DateTime:yyyy-MM-dd}.");

            await quotesCache.Put(missingFundHistory);
        }
    }


    public static Task<Quote?> GetMissingHistory(Quote history, out DateTime start, out DateTime end)
    {
        ArgumentNullException.ThrowIfNull(history);

        var lastDate = history.Prices[^1].DateTime.Date;

        start = lastDate.AddDays(1);
        end = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);

        if (!(start < end))
        {
            return Task.FromResult<Quote?>(null);
        }

        return QuotesController.GetHistoryRange(history.Ticker, start, end);
    }

    public static Task<Quote?> GetHistoryAll(string ticker)
    {
        return QuotesController.GetHistoryRange(ticker, new DateTime(1900, 1, 1), DateTime.UtcNow);
    }

    private static async Task<Quote?> GetHistoryRange(string ticker, DateTime start, DateTime end)
    {
        static async Task<T> throttle<T>(Func<Task<T>> operation)
        {
            await Task.Delay(1000);
            return await operation();
        }

        if (start >= end)
        {
            return null;
        }

        var history = new Quote(ticker)
        {
            Dividends = (await throttle(() => Yahoo.GetDividendsAsync(ticker, start, end)))
                .Select(divTick => new QuoteDividendRecord(divTick))
                .ToList(),
            Prices = (await throttle(() => Yahoo.GetHistoricalAsync(ticker, start, end)))
                .Select(candle => new QuotePriceRecord(candle))
                .ToList(),
            Splits = (await throttle(() => Yahoo.GetSplitsAsync(ticker, start, end)))
                .Select(splitTick => new QuoteSplitRecord(splitTick))
                .ToList()
        };

        // API will sometimes return zeroed records if `end` is today
        if (history.Prices[^1].Open == 0)
        {
            history.Prices.RemoveAt(history.Prices.Count - 1);
        }

        return history;
    }
}
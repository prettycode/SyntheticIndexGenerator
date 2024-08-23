using Data.Quotes.QuoteProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableFileCache;

namespace Data.Quotes;

internal class QuotesService(
    IQuoteRepository quoteRepository,
    IQuoteProvider quoteProvider,
    IOptions<QuotesServiceOptions> quoteServiceOptions,
    ILogger<QuotesService> logger)
        : IQuotesService
{
    private static readonly object downloadLocker = new();

    private readonly bool fromCacheOnly = quoteServiceOptions.Value.GetQuotesFromCacheOnly;

    private readonly AbsoluteExpirationCache<string, Task<Quote>> getQuoteTasks = new(()
        => DateTimeOffset.Now.AddMinutes(1));

    public async Task<IEnumerable<QuotePrice>> GetDailyQuoteHistory(string ticker)
        => (await GetQuoteTask(ticker)).Prices;

    private Task<Quote> GetQuoteTask(string ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        if (getQuoteTasks.TryGetValue(ticker, out Task<Quote>? getQuoteTask))
        {
            return getQuoteTask ?? throw new InvalidOperationException(
                $"{ticker}: Trying to get quote task was successful but task was, and should not be, null.");
        }

        return getQuoteTasks[ticker] = Task.Run(() => GetQuote(ticker)).ContinueWith(quote
            => quote.Result ?? throw new KeyNotFoundException($"{ticker}: No history found."));
    }

    private async Task<Quote> GetQuote(string ticker)
    {
        logger.LogInformation("{ticker}: Getting quote from repository…", ticker);

        var knownHistory = await quoteRepository.TryGetQuote(ticker);

        if (knownHistory != null)
        {
            logger.LogInformation("{ticker}: {recordCount} record(s) in cache, {firstPeriod} to {lastPeriod}.",
                ticker,
                knownHistory.Prices.Count,
                $"{knownHistory.Prices[0].DateTime:yyyy-MM-dd}",
                $"{knownHistory.Prices[^1].DateTime:yyyy-MM-dd}");

            return knownHistory;
        }

        if (fromCacheOnly)
        {
            throw new InvalidOperationException($"{ticker} quote not found in cache.");
        }

        logger.LogInformation("{ticker}: No quote in cache.", ticker);

        var entireHistory = await DownloadEntireHistory(ticker);

        logger.LogInformation("{ticker}: Writing {recordCount} record(s) to cache, {firstPeriod} to {lastPeriod}.",
            ticker,
            entireHistory.Prices.Count,
            $"{entireHistory.Prices[0].DateTime:yyyy-MM-dd}",
            $"{entireHistory.Prices[^1].DateTime:yyyy-MM-dd}");

        return await quoteRepository.PutQuote(entireHistory);
    }

    private async Task<Quote> DownloadEntireHistory(string ticker)
        => await DownloadQuote(ticker) ?? throw new KeyNotFoundException($"{ticker}: No history found.");

    private Task<Quote?> DownloadQuote(string ticker, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Across all the threads that want to download a quote, only allow one thread to do so at a time.
        // Temporary measure to avoid rate-limiting against quote provider or accidentally DoSing.

        Quote? downloadedQuote;

        logger.LogInformation("{ticker}: Downloading entire history…", ticker);

        lock (downloadLocker)
        {
            Task.Delay(1000).GetAwaiter().GetResult();
            downloadedQuote = quoteProvider.GetQuote(ticker, startDate, endDate).GetAwaiter().GetResult();
        }

        logger.LogInformation("{ticker}: …done downloading entire history.", ticker);

        // Make sure function is still a Task; when Yahoo Finance is replaced with an actual data provider, we'll
        // be able to eliminate the locker in this function.
        return Task.FromResult(downloadedQuote);
    }

    /// <summary>
    /// Check for new records to add to the history and return that if there are any, or get the entire history
    /// because historical records have changed (e.g. adjusted close has been recalculated).
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<(bool ReplaceExistingData, Quote? NewHistory)> GetNewQuote(Quote fundHistory)
    {
        var ticker = fundHistory.Ticker;
        var staleHistoryLastTick = fundHistory.Prices[^1];
        var staleHistoryLastTickDate = staleHistoryLastTick.DateTime;

        logger.LogInformation("{ticker}: Downloading history starting at {staleHistoryLastTickDate}...",
            ticker,
            $"{staleHistoryLastTickDate:yyyy-MM-dd}");

        var freshHistory = await DownloadQuote(ticker, staleHistoryLastTickDate);

        if (freshHistory == null)
        {
            return (false, null);
        }

        if (freshHistory.Prices[0].DateTime != staleHistoryLastTickDate)
        {
            throw new InvalidOperationException($"{ticker}: Fresh history should start on last date of existing history.");
        }

        var firstFresh = freshHistory.Prices[0];

        if (firstFresh.Open != staleHistoryLastTick.Open ||
            firstFresh.Close != staleHistoryLastTick.Close ||
            firstFresh.AdjustedClose != staleHistoryLastTick.AdjustedClose)
        {
            logger.LogWarning("{ticker}: All history has been recomputed.", ticker);

            return (true, await DownloadEntireHistory(ticker));
        }

        freshHistory.Prices.RemoveAt(0);

        if (freshHistory.Prices.Count == 0)
        {
            return (false, null);
        }

        if (freshHistory.Dividends.Count > 0 &&
            freshHistory.Dividends[0].DateTime == fundHistory.Dividends[^1].DateTime)
        {
            freshHistory.Dividends.RemoveAt(0);
        }

        if (freshHistory.Splits.Count > 0 &&
            freshHistory.Splits[0].DateTime == fundHistory.Splits[^1].DateTime)
        {
            freshHistory.Splits.RemoveAt(0);
        }

        return (false, freshHistory);
    }
}
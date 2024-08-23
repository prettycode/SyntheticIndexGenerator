using System.Collections.Concurrent;
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

    private readonly DailyExpirationCache<string, Task<Quote>> downloadTasks = new(()
        => DateTimeOffset.Now.AddMinutes(1));

    public async Task<Dictionary<string, IEnumerable<QuotePrice>>> GetDailyQuoteHistory(HashSet<string> tickers)
    {
        ArgumentNullException.ThrowIfNull(tickers);

        // TODO this is not parallelized; don't have awaits in here
        return await tickers
            .ToAsyncEnumerable()
            .SelectAwait(async ticker => new { ticker, quote = await GetDailyQuoteHistory(ticker) })
            .ToDictionaryAsync(pair => pair.ticker, pair => pair.quote);
    }

    public async Task<IEnumerable<QuotePrice>> GetDailyQuoteHistory(string ticker) => (await GetQuote(ticker)).Prices;

    private async Task<Quote> GetQuote(string ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);

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

        logger.LogInformation("{ticker}: No quote in cache. Downloading entire history…", ticker);

        var entireHistory = await DownloadEntireHistory(ticker);

        logger.LogInformation("{ticker}: Writing {recordCount} record(s) to cache, {firstPeriod} to {lastPeriod}.",
            ticker,
            entireHistory.Prices.Count,
            $"{entireHistory.Prices[0].DateTime:yyyy-MM-dd}",
            $"{entireHistory.Prices[^1].DateTime:yyyy-MM-dd}");

        return await quoteRepository.PutQuote(entireHistory);
    }

    private Task<Quote> DownloadEntireHistory(string ticker)
    {
        if (downloadTasks.TryGetValue(ticker, out Task<Quote>? downloadTask))
        {
            return downloadTask ?? throw new InvalidOperationException();
        }

        return downloadTasks[ticker] = Task.Run(() => DownloadQuote(ticker)).ContinueWith(quote
            => quote.Result ?? throw new KeyNotFoundException($"{ticker}: No history found."));
    }

    private Quote? DownloadQuote(string ticker, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Across all the threads that want to download a quote, only allow one thread to do so at a time.
        // Temporary measure to avoid rate-limiting against quote provider or accidentally DoSing.

        Quote? downloadedQuote;

        lock (downloadLocker)
        {
            Task.Delay(1000).GetAwaiter().GetResult();
            downloadedQuote = quoteProvider.GetQuote(ticker, startDate, endDate).GetAwaiter().GetResult();
        }

        return downloadedQuote;
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

        var freshHistory = DownloadQuote(ticker, staleHistoryLastTickDate);

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
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

    private readonly bool skipDownloadingUncachedQuotes = quoteServiceOptions.Value.SkipDownloadingUncachedQuotes;

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
        logger.LogInformation("{ticker}: Getting quote from repository...", ticker);

        var memoryCacheQuote = await quoteRepository.TryGetMemoryCacheQuote(ticker);

        if (memoryCacheQuote != null)
        {
            return memoryCacheQuote;
        }

        var fileCacheQuote = await quoteRepository.TryGetFileCacheQuote(ticker);


        if (skipDownloadingUncachedQuotes)
        {
            if (fileCacheQuote == null)
            {
                throw new InvalidOperationException($"{ticker} quote not found in cache.");
            }

            return await quoteRepository.PutQuote(fileCacheQuote);
        }

        if (fileCacheQuote == null)
        {
            var freshQuote = await DownloadFreshQuote(ticker);

            return await quoteRepository.PutQuote(freshQuote);
        }

        var upToDateQuote = await UpdateStaleQuote(fileCacheQuote);

        return await quoteRepository.PutQuote(upToDateQuote);
    }

    private async Task<Quote> UpdateStaleQuote(Quote staleQuote)
    {
        var ticker = staleQuote.Ticker;
        var staleHistoryLastTick = staleQuote.Prices[^1];
        var staleHistoryLastTickDate = staleHistoryLastTick.DateTime;

        logger.LogInformation("{ticker}: Downloading history starting at {staleHistoryLastTickDate}...",
            ticker,
            $"{staleHistoryLastTickDate:yyyy-MM-dd}");

        var deltaQuote = await DownloadQuote(ticker, staleHistoryLastTickDate);

        if (deltaQuote == null)
        {
            return new Quote(ticker)
            {
                Dividends = staleQuote.Dividends,
                Prices = staleQuote.Prices,
                Splits = staleQuote.Splits
            };
        }

        if (deltaQuote.Dividends == null)
        {
            throw new InvalidOperationException("Expected an empty enumerable for dividends.");
        }

        if (deltaQuote.Prices == null)
        {
            throw new InvalidOperationException("Expected an empty enumerable for prices.");
        }

        if (deltaQuote.Splits == null)
        {
            throw new InvalidOperationException("Expected an empty enumerable for splits.");
        }

        if (deltaQuote.Prices[0].DateTime != staleHistoryLastTickDate)
        {
            throw new InvalidOperationException($"{ticker}: Fresh history should start on last date of existing history.");
        }

        var firstDeltaPrice = deltaQuote.Prices[0];

        if (firstDeltaPrice.Open != staleHistoryLastTick.Open ||
            firstDeltaPrice.Close != staleHistoryLastTick.Close ||
            firstDeltaPrice.AdjustedClose != staleHistoryLastTick.AdjustedClose)
        {
            logger.LogWarning("{ticker}: All history has changed.", ticker);

            return await DownloadFreshQuote(ticker);
        }

        deltaQuote.Prices.RemoveAt(0);

        if (deltaQuote.Prices.Count == 0)
        {
            return staleQuote;
        }

        if (deltaQuote.Dividends.Count > 0 &&
            deltaQuote.Dividends[0].DateTime == staleQuote.Dividends[^1].DateTime)
        {
            deltaQuote.Dividends.RemoveAt(0);
        }

        if (deltaQuote.Splits.Count > 0 &&
            deltaQuote.Splits[0].DateTime == staleQuote.Splits[^1].DateTime)
        {
            deltaQuote.Splits.RemoveAt(0);
        }

        return new Quote(ticker)
        {
            Dividends = [.. staleQuote.Dividends, .. deltaQuote.Dividends],
            Prices = [.. staleQuote.Prices, .. deltaQuote.Prices],
            Splits = [.. staleQuote.Splits, .. deltaQuote.Splits]
        };
    }

    private async Task<Quote> DownloadFreshQuote(string ticker)
        => await DownloadQuote(ticker) ?? throw new KeyNotFoundException($"{ticker}: No history found.");

    private Task<Quote?> DownloadQuote(string ticker, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Across all the threads that want to download a quote, only allow one thread to do so at a time.
        // Temporary measure to avoid rate-limiting against quote provider or accidentally DoSing.

        Quote? downloadedQuote;


        logger.LogInformation("{ticker}: Downloading history {startDate} to {endDate}...",
            ticker,
            $"{startDate:yyyy-MM-dd}",
            $"{endDate:yyyy-MM-dd}");

        lock (downloadLocker)
        {
            Task.Delay(1000).GetAwaiter().GetResult();
            downloadedQuote = quoteProvider.GetQuote(ticker, startDate, endDate).GetAwaiter().GetResult();
        }

        logger.LogInformation("{ticker}: ...done downloading history.", ticker);

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

            return (true, await DownloadFreshQuote(ticker));
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
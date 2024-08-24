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

            return fileCacheQuote;
        }

        if (fileCacheQuote == null)
        {
            var freshQuote = await DownloadFreshQuote(ticker);

            return await quoteRepository.PutQuote(freshQuote);
        }

        (bool isChanged, Quote upToDateQuote) = await UpdateStaleQuote(fileCacheQuote);

        if (!isChanged)
        {
            return fileCacheQuote;
        }

        return await quoteRepository.PutQuote(upToDateQuote);
    }

    private async Task<(bool IsChanged, Quote UpToDateQuopte)> UpdateStaleQuote(Quote staleQuote)
    {
        var ticker = staleQuote.Ticker;
        var staleHistoryLastTick = staleQuote.Prices[^1];
        var staleHistoryLastTickDate = staleHistoryLastTick.DateTime;
        var deltaQuote = await DownloadQuote(ticker, staleHistoryLastTickDate);

        if (deltaQuote == null)
        {
            logger.LogInformation("{ticker}: Download had no new history.", ticker);

            return (false, new Quote(ticker)
            {
                Dividends = staleQuote.Dividends,
                Prices = staleQuote.Prices,
                Splits = staleQuote.Splits
            });
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
            logger.LogWarning("{ticker}: All history has changed. Downloading entire history...", ticker);

            return (true, await DownloadFreshQuote(ticker));
        }

        deltaQuote.Prices.RemoveAt(0);

        if (deltaQuote.Prices.Count == 0)
        {
            logger.LogInformation("{ticker}: Download had no new history.", ticker);

            return (false, staleQuote);
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

        logger.LogInformation("{ticker}: Download had {newRecords}, {startDate} to {endDate}.",
            ticker,
            $"{deltaQuote.Prices.Count}",
            $"{deltaQuote.Prices[0].DateTime:yyyy-MM-dd}",
            $"{deltaQuote.Prices[^1].DateTime:yyyy-MM-dd}");

        return (true, new Quote(ticker)
        {
            Dividends = [.. staleQuote.Dividends, .. deltaQuote.Dividends],
            Prices = [.. staleQuote.Prices, .. deltaQuote.Prices],
            Splits = [.. staleQuote.Splits, .. deltaQuote.Splits]
        });
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

        // Make sure function is still a Task; when Yahoo Finance is replaced with an actual data provider, we'll
        // be able to eliminate the locker in this function.
        return Task.FromResult(downloadedQuote);
    }
}
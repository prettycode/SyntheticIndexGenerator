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
    private static readonly Lock downloadLocker = new();

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

        return getQuoteTasks[ticker] = GetQuote(ticker);
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

        if (quoteServiceOptions.Value.SkipDownloadingUncachedQuotes)
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

        (bool isChanged, Quote upToDateQuote) = await GetUpdatedQuoteFromStale(fileCacheQuote);

        if (!isChanged)
        {
            return fileCacheQuote;
        }

        return await quoteRepository.PutQuote(upToDateQuote);
    }

    private async Task<(bool IsChanged, Quote UpToDateQuote)> GetUpdatedQuoteFromStale(Quote staleQuote)
    {
        var ticker = staleQuote.Ticker;
        var staleHistoryLastTick = staleQuote.Prices[^1];
        var staleHistoryLastTickDate = staleHistoryLastTick.DateTime;
        var staleQuoteCopy = new Quote(ticker)
        {
            Dividends = staleQuote.Dividends,
            Prices = staleQuote.Prices,
            Splits = staleQuote.Splits
        };

        if (!IsNewDataLikelyAvailable(staleHistoryLastTickDate))
        {
            logger.LogInformation(
                "{ticker}: The market has not traded since {endDate}. Skipping new data download.",
                ticker,
                $"{staleHistoryLastTickDate:yyyy-MM-dd}");

            return (false, staleQuoteCopy);
        }

        var deltaQuote = await DownloadQuote(ticker, staleHistoryLastTickDate);

        if (deltaQuote == null)
        {
            logger.LogInformation("{ticker}: Download had no new history.", ticker);

            return (false, staleQuoteCopy);
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

        if (firstDeltaPrice.AdjustedClose != staleHistoryLastTick.AdjustedClose)
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

        logger.LogInformation("{ticker}: Download had {newRecords} new record(s), {startDate} to {endDate}.",
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

    private static bool IsNewDataLikelyAvailable(DateTime lastDataPoint)
    {
        var newYorkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var lastDataPointDateNewYork = TimeZoneInfo.ConvertTime(lastDataPoint, newYorkTimeZone).Date;
        var currentDateNewYork = TimeZoneInfo.ConvertTime(DateTime.UtcNow, newYorkTimeZone).Date;

        if (lastDataPointDateNewYork > currentDateNewYork)
        {
            throw new InvalidOperationException("Last date should not be more recent than today.");
        }

        var nextBusinessDay = lastDataPointDateNewYork.AddDays(1);

        while (nextBusinessDay.DayOfWeek == DayOfWeek.Saturday || nextBusinessDay.DayOfWeek == DayOfWeek.Sunday)
        {
            nextBusinessDay = nextBusinessDay.AddDays(1);
        }

        return nextBusinessDay < currentDateNewYork;
    }

    private async Task<Quote> DownloadFreshQuote(string ticker)
        => await DownloadQuote(ticker) ?? throw new KeyNotFoundException($"{ticker}: No history found.");

    private Task<Quote?> DownloadQuote(string ticker, DateTime? startDate = null, DateTime? endDate = null)
    {
        logger.LogInformation("{ticker}: Downloading history {startDate} to {endDate}...",
            ticker,
            startDate?.ToString("yyyy-MM-dd") ?? "null",
            endDate?.ToString("yyyy-MM-dd") ?? "null");

        if (!quoteProvider.RunGetQuoteSingleThreaded)
        {
            return quoteProvider.GetQuote(ticker, startDate, endDate);
        }

        // Across all the threads that want to download a quote, only allow one thread to do so at a time.
        // Used to avoid rate-limiting against quote provider or accidentally DoSing.

        lock (downloadLocker)
        {
            Task.Delay(quoteServiceOptions.Value.ThrottleQuoteProviderRequestsMs)
                .GetAwaiter()
                .GetResult();

            // Run synchronously while in locker and return Task wrapper

            return Task.FromResult(
                quoteProvider
                    .GetQuote(ticker, startDate, endDate)
                    .GetAwaiter()
                    .GetResult());
        }
    }
}
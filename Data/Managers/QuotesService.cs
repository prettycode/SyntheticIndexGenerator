using Data.Models;
using Data.QuoteProvider;
using Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Data.Controllers
{
    internal class QuotesService(IQuoteRepository quoteRepository, IQuoteProvider quoteProvider, ILogger<QuotesService> logger) : IQuotesService
    {
        private IQuoteRepository QuoteCache { get; init; } = quoteRepository;

        private IQuoteProvider QuoteProvider { get; init; } = quoteProvider;

        private ILogger<QuotesService> Logger { get; init; } = logger;

        public async Task<IEnumerable<QuotePrice>> GetPriceHistory(string ticker) => (await GetQuote(ticker)).Prices;

        public async Task<Dictionary<string, Quote>> GetQuotes(HashSet<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            return await tickers.ToAsyncEnumerable()
                .SelectAwait(async ticker => new { ticker, quote = await GetQuote(ticker) })
                .ToDictionaryAsync(pair => pair.ticker, pair => pair.quote);
        }

        private async Task<Quote> GetQuote(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            // Check the cache for an entry

            var knownHistory = QuoteCache.Has(ticker) ? await QuoteCache.Get(ticker) : null;

            if (knownHistory == null)
            {
                // Not in cache, so download the entire history and cache it

                Logger.LogInformation("{ticker}: No history found in cache.", ticker);

                var allHistory = await GetAllHistory(ticker);

                Logger.LogInformation("{ticker}: Writing {recordCount} record(s) to cache, {startDate} to {endDate}.",
                    ticker,
                    allHistory.Prices.Count,
                    $"{allHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{allHistory.Prices[^1].DateTime:yyyy-MM-dd}");

                return await QuoteCache.Replace(allHistory);
            }
            else
            {
                Logger.LogInformation("{ticker}: {recordCount} record(s) in cache, {startDate} to {endDate}.",
                    ticker,
                    knownHistory.Prices.Count,
                    $"{knownHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{knownHistory.Prices[^1].DateTime:yyyy-MM-dd}");
            }

            // It's in the cache, but may be outdated, so check for new data

            var (replaceExistingHistory, newHistory) = await GetNewQuote(knownHistory);

            // It's not outdated

            if (newHistory == null)
            {
                Logger.LogInformation("{ticker}: No new history found.", ticker);

                return knownHistory;
            }

            // It's outdated; there's either new records to append, or the entire history has changed and needs replacing

            if (!replaceExistingHistory)
            {
                Logger.LogInformation("{ticker}: Missing history identified as {startDate} to {endDate}",
                    ticker,
                    $"{newHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{newHistory.Prices[^1].DateTime:yyyy-MM-dd}");
            }

            Logger.LogInformation("{ticker}: Writing {recordCount} record(s) to cache, {startDate} to {endDate}",
                    ticker,
                    newHistory.Prices.Count,
                    $"{newHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{newHistory.Prices[^1].DateTime:yyyy-MM-dd}");

            return replaceExistingHistory
                ? await QuoteCache.Replace(newHistory)
                : await QuoteCache.Append(newHistory);
        }

        private async Task<Quote> GetAllHistory(string ticker)
        {
            Logger.LogInformation("{ticker}: Downloading all history...", ticker);

            var allHistory = await DownloadQuote(ticker)
                ?? throw new InvalidOperationException($"{ticker}: No history found."); ;

            return allHistory;
        }

        /// <summary>
        /// Check for new records to add to the history and return that if there are any, or get the entire history
        /// because historical records have changed (e.g. adjusted close has been recalculated).
        /// <exception cref="InvalidOperationException"></exception>
        // TODO test
        private async Task<(bool ReplaceExistingData, Quote? NewHistory)> GetNewQuote(Quote fundHistory)
        {
            var ticker = fundHistory.Ticker;
            var staleHistoryLastTick = fundHistory.Prices[^1];
            var staleHistoryLastTickDate = staleHistoryLastTick.DateTime;

            Logger.LogInformation("{ticker}: Downloading history starting at {staleHistoryLastTickDate}...",
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
                Logger.LogWarning("{ticker}: All history has been recomputed.", ticker);

                return (true, await GetAllHistory(ticker));
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

        private async Task<Quote?> DownloadQuote(string ticker, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await QuoteProvider.GetQuote(ticker, startDate, endDate);
        }
    }
}
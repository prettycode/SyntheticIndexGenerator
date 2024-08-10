using Data.Models;
using Data.QuoteProvider;
using Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Data.Services
{
    internal class QuotesService(IQuoteRepository quoteRepository, IQuoteProvider quoteProvider, ILogger<QuotesService> logger) : IQuotesService
    {
        public async Task<Dictionary<string, IEnumerable<QuotePrice>>> GetPrices(
            HashSet<string> tickers,
            bool skipRefresh = false)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            return await tickers
                .ToAsyncEnumerable()
                .SelectAwait(async ticker => new { ticker, quote = await GetQuotePrices(ticker, skipRefresh) })
                .ToDictionaryAsync(pair => pair.ticker, pair => pair.quote);
        }

        public async Task<Dictionary<string, Dictionary<string, IEnumerable<QuotePrice>>>> GetSyntheticPrices(
            HashSet<string> tickers,
            bool skipRefresh = false)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            var constituentTickersBySyntheticTicker = SyntheticIndex
                .GetSyntheticIndices()
                .ToDictionary(index => index.Ticker, index => index.BackFillTickers);

            Dictionary<string, Dictionary<string, IEnumerable<QuotePrice>>> syntheticConstituentDailyPricesByTicker = [];

            foreach (var syntheticTicker in tickers)
            {
                if (!constituentTickersBySyntheticTicker.TryGetValue(syntheticTicker, out var backFillTickers))
                {
                    throw new ArgumentException($"Synthetic ticker '{syntheticTicker}' not found.", nameof(tickers));
                }

                var nonSyntheticConstituentTickers = backFillTickers.Where(t => !t.StartsWith('$'));

                var quotePrices = await Task.WhenAll(nonSyntheticConstituentTickers.Select(ticker
                    => GetQuotePrices(ticker, skipRefresh)));

                syntheticConstituentDailyPricesByTicker[syntheticTicker] = nonSyntheticConstituentTickers
                    .Zip(quotePrices)
                    .ToDictionary(pair => pair.First, pair => pair.Second);
            }

            return syntheticConstituentDailyPricesByTicker;
        }

        private async Task<IEnumerable<QuotePrice>> GetQuotePrices(string ticker, bool skipRefresh)
            => (await GetQuote(ticker, skipRefresh)).Prices;

        private async Task<Dictionary<string, Quote>> GetQuotes(HashSet<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            return await tickers
                .ToAsyncEnumerable()
                .SelectAwait(async ticker => new { ticker, quote = await GetQuote(ticker, false) })
                .ToDictionaryAsync(pair => pair.ticker, pair => pair.quote);
        }

        private async Task<Quote> GetQuote(string ticker, bool skipRefresh)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            // Check the cache for an entry

            var knownHistory = quoteRepository.Has(ticker) ? await quoteRepository.Get(ticker) : null;

            if (skipRefresh)
            {
                if (knownHistory == null)
                {
                    throw new InvalidOperationException($"{ticker} quote not found in cache.");
                }

                logger.LogInformation("{ticker}: {recordCount} record(s) in cache, {firstPeriod} to {lastPeriod}.",
                    ticker,
                    knownHistory.Prices.Count,
                    $"{knownHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{knownHistory.Prices[^1].DateTime:yyyy-MM-dd}");

                return knownHistory;
            }

            if (knownHistory == null)
            {
                // Not in cache, so download the entire history and cache it

                logger.LogInformation("{ticker}: No history found in cache.", ticker);

                var allHistory = await GetAllHistory(ticker);

                logger.LogInformation("{ticker}: Writing {recordCount} record(s) to cache, {firstPeriod} to {lastPeriod}.",
                    ticker,
                    allHistory.Prices.Count,
                    $"{allHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{allHistory.Prices[^1].DateTime:yyyy-MM-dd}");

                return await quoteRepository.Replace(allHistory);
            }
            else
            {
                logger.LogInformation("{ticker}: {recordCount} record(s) in cache, {firstPeriod} to {lastPeriod}.",
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
                logger.LogInformation("{ticker}: No new history found.", ticker);

                return knownHistory;
            }

            // It's outdated; there's either new records to append, or the entire history has changed and needs replacing

            if (!replaceExistingHistory)
            {
                logger.LogInformation("{ticker}: Missing history identified as {firstPeriod} to {lastPeriod}",
                    ticker,
                    $"{newHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{newHistory.Prices[^1].DateTime:yyyy-MM-dd}");
            }

            logger.LogInformation("{ticker}: Writing {recordCount} record(s) to cache, {firstPeriod} to {lastPeriod}",
                    ticker,
                    newHistory.Prices.Count,
                    $"{newHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{newHistory.Prices[^1].DateTime:yyyy-MM-dd}");

            return replaceExistingHistory
                ? await quoteRepository.Replace(newHistory)
                : await quoteRepository.Append(newHistory);
        }

        private async Task<Quote> GetAllHistory(string ticker)
        {
            logger.LogInformation("{ticker}: Downloading all history...", ticker);

            var allHistory = await DownloadQuote(ticker)
                ?? throw new InvalidOperationException($"{ticker}: No history found."); ;

            return allHistory;
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
            return await quoteProvider.GetQuote(ticker, startDate, endDate);
        }
    }
}
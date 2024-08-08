using Data.Models;
using Data.QuoteProvider;
using Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Data.Controllers
{
    public class QuotesService(IQuoteRepository quoteRepository, IQuoteProvider quoteProvider, ILogger<QuotesService> logger)
    {
        private IQuoteRepository QuoteCache { get; init; } = quoteRepository;

        private IQuoteProvider QuoteProvider { get; init; } = quoteProvider;

        private ILogger<QuotesService> Logger { get; init; } = logger;

        public async Task<Dictionary<string, Quote?>> GetQuotes(HashSet<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            return await tickers.ToAsyncEnumerable()
                .SelectAwait(async ticker => new { ticker, quote = await GetQuote(ticker) })
                .ToDictionaryAsync(pair => pair.ticker, pair => pair.quote);
        }

        public async Task<Quote?> GetQuote(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            var knownHistory = QuoteCache.Has(ticker) ? await QuoteCache.Get(ticker) : null;

            if (knownHistory == null)
            {
                Logger.LogInformation("{ticker}: No history found in cache.", ticker);

                var allHistory = await GetAllHistory(ticker);

                Logger.LogInformation("{ticker}: Writing {recordCount} record(s) to cache, {startDate} to {endDate}.",
                    ticker,
                    allHistory.Prices.Count,
                    $"{allHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{allHistory.Prices[^1].DateTime:yyyy-MM-dd}");

                await QuoteCache.Append(allHistory);

                return allHistory;
            }
            else
            {
                Logger.LogInformation("{ticker}: {recordCount} record(s) in cache, {startDate} to {endDate}.",
                    ticker,
                    knownHistory.Prices.Count,
                    $"{knownHistory.Prices[0].DateTime:yyyy-MM-dd}",
                    $"{knownHistory.Prices[^1].DateTime:yyyy-MM-dd}");
            }

            var (isAllHistory, newHistory) = await GetNewHistory(knownHistory);

            if (newHistory == null)
            {
                Logger.LogInformation("{ticker}: No new history found.", ticker);

                return null;
            }

            if (!isAllHistory)
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

            var updateTask = isAllHistory
                ? QuoteCache.Replace(newHistory)
                : QuoteCache.Append(newHistory);

            await updateTask;

            return newHistory;
        }

        private async Task<Quote> GetAllHistory(string ticker)
        {
            Logger.LogInformation("{ticker}: Downloading all history...", ticker);

            var allHistory = await DownloadQuote(ticker)
                ?? throw new InvalidOperationException($"{ticker}: No history found."); ;

            return allHistory;
        }

        // TODO test
        private async Task<(bool, Quote?)> GetNewHistory(Quote fundHistory)
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
            return await quoteProvider.GetQuote(ticker, startDate, endDate);
        }
    }
}
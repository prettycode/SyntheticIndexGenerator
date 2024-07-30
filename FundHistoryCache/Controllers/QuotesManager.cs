using FundHistoryCache.Models;
using FundHistoryCache.Repositories;
using Microsoft.Extensions.Logging;

namespace FundHistoryCache.Controllers
{
    public class QuotesManager(QuoteRepository quoteRepository, ILogger<QuotesManager> logger)
    {
        private QuoteRepository QuoteCache { get; init; } = quoteRepository;
        private ILogger<QuotesManager> Logger { get; init; } = logger;

        public async Task RefreshQuotes(HashSet<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            // Do serially vs. all at once; avoid rate-limiting
            foreach (var ticker in tickers)
            {
                Logger.LogInformation("{ticker}: Refreshing history...", ticker);

                try
                {
                    await RefreshQuote(ticker);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "{ticker}: Refresh failed.", ticker);
                }
            }
        }

        public async Task RefreshQuote(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            var knownHistory = await QuoteCache.Get(ticker);

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

                return;
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
                return;
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
        }

        private async Task<Quote> GetAllHistory(string ticker)
        {
            Logger.LogInformation("{ticker}: Downloading all history...", ticker);

            var allHistory = await GetQuote(ticker)
                ?? throw new InvalidOperationException($"{ticker}: No history found."); ;

            return allHistory;
        }

        private async Task<(bool, Quote?)> GetNewHistory(Quote fundHistory)
        {
            var ticker = fundHistory.Ticker;

            Logger.LogInformation("{ticker}: Downloading new history...", ticker);

            var staleHistoryLastTick = fundHistory.Prices[^1];
            var staleHistoryLastTickDate = staleHistoryLastTick.DateTime;
            var staleHistoryLastTickOpen = staleHistoryLastTick.Open;
            var freshHistory = await GetQuote(ticker, staleHistoryLastTickDate);

            if (freshHistory == null)
            {
                return (false, null);
            }

            if (freshHistory.Prices[0].DateTime != staleHistoryLastTickDate)
            {
                throw new InvalidOperationException($"{ticker}: Fresh history should start on last date of existing history.");
            }

            if (freshHistory.Prices[0].Open != staleHistoryLastTickOpen)
            {
                Logger.LogInformation("{ticker}: All history has been recomputed.", ticker);

                return (true, await GetAllHistory(ticker));
            }

            freshHistory.Prices.RemoveAt(0);

            if (freshHistory.Prices.Count == 0)
            {
                return (false, null);
            }

            if (freshHistory.Dividends.Count > 0 && freshHistory.Dividends[0].DateTime == fundHistory.Dividends[^1].DateTime)
            {
                freshHistory.Dividends.RemoveAt(0);
            }

            if (freshHistory.Splits.Count > 0 && freshHistory.Splits[0].DateTime == fundHistory.Splits[^1].DateTime)
            {
                freshHistory.Splits.RemoveAt(0);
            }

            return (false, freshHistory);
        }

        private static async Task<Quote?> GetQuote(string ticker, DateTime? startDate = null, DateTime? endDate = null)
        {
            static async Task<T> Throttle<T>(Func<Task<T>> operation)
            {
                await Task.Delay(2500);
                return await operation();
            }

            var dividends = (await Throttle(() => YahooFinanceApi.Yahoo.GetDividendsAsync(ticker, startDate, endDate))).ToList();
            var prices = (await Throttle(() => YahooFinanceApi.Yahoo.GetHistoricalAsync(ticker, startDate, endDate))).ToList();
            var splits = (await Throttle(() => YahooFinanceApi.Yahoo.GetSplitsAsync(ticker, startDate, endDate))).ToList();

            // API returns a record with 0s when record is today and not yet updated after market close

            if (prices[^1].Open == 0)
            {
                var incompleteDate = prices[^1].DateTime;

                prices.RemoveAt(prices.Count - 1);

                if (prices.Count == 0)
                {
                    return null;
                }

                if (dividends.Count > 0 && dividends[^1].DateTime == incompleteDate)
                {
                    dividends.RemoveAt(dividends.Count - 1);
                }

                if (splits.Count > 0 && splits[^1].DateTime == incompleteDate)
                {
                    splits.RemoveAt(splits.Count - 1);
                }
            }

            return new Quote(ticker)
            {
                Dividends = dividends.Select(div => new QuoteDividend(div)).ToList(),
                Prices = prices.Select(price => new QuotePrice(price)).ToList(),
                Splits = splits.Select(split => new QuoteSplit(split)).ToList()
            };
        }
    }
}
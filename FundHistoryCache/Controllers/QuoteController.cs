using System.Collections.Frozen;
using FundHistoryCache.Models;
using FundHistoryCache.Repositories;

namespace FundHistoryCache.Controllers
{
    public static class QuoteController
    {

        public static async Task RefreshQuotes(QuoteRepository quotesCache, HashSet<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(quotesCache);
            ArgumentNullException.ThrowIfNull(tickers);

            // Do serially vs. all at once; avoid rate-limiting
            foreach (var ticker in tickers)
            {
                await RefreshQuote(quotesCache, ticker);
            }
        }

        public static async Task RefreshQuote(QuoteRepository quotesCache, string ticker)
        {
            ArgumentNullException.ThrowIfNull(quotesCache);
            ArgumentNullException.ThrowIfNull(ticker);

            var knownHistory = await quotesCache.Get(ticker);

            if (knownHistory == null)
            {
                var allHistory = await GetAllHistory(ticker);
                await quotesCache.Append(allHistory);
                return;
            }

            var (isAllHistory, newHistory) = await GetNewHistory(knownHistory);

            if (newHistory == null)
            {
                return;
            }

            var updateTask = isAllHistory
                ? quotesCache.Replace(newHistory)
                : quotesCache.Append(newHistory);

            await updateTask;
        }

        private static async Task<Quote> GetAllHistory(string ticker) {
            var allHistory = await GetQuote(ticker)
                ?? throw new InvalidOperationException($"{ticker}: No history found."); ;

            return allHistory;
        }

        private static async Task<(bool, Quote?)> GetNewHistory(Quote fundHistory)
        {
            var ticker = fundHistory.Ticker;
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

            return (false,  freshHistory);
        }

        private static async Task<Quote?> GetQuote(string ticker, DateTime? startDate = null, DateTime? endDate = null)
        {
            static async Task<T> throttle<T>(Func<Task<T>> operation)
            {
                await Task.Delay(2500);
                return await operation();
            }

            var dividends = (await throttle(() => YahooFinanceApi.Yahoo.GetDividendsAsync(ticker, startDate, endDate))).ToList();
            var prices = (await throttle(() => YahooFinanceApi.Yahoo.GetHistoricalAsync(ticker, startDate, endDate))).ToList();
            var splits = (await throttle(() => YahooFinanceApi.Yahoo.GetSplitsAsync(ticker, startDate, endDate))).ToList();

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
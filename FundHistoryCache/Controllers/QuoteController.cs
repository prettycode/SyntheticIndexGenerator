using FundHistoryCache.Models;
using FundHistoryCache.Repositories;

namespace FundHistoryCache.Controllers
{
    public static class QuoteController
    {
        public static async Task<bool> RefreshQuote(QuoteRepository quotesCache, string ticker)
        {
            ArgumentNullException.ThrowIfNull(quotesCache);
            ArgumentNullException.ThrowIfNull(ticker);

            var fundHistory = await quotesCache.Get(ticker);

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history found in cache.");
            }
            else
            {
                Console.WriteLine($"{ticker}: {fundHistory.Prices.Count} record(s) in cache, {fundHistory.Prices[0].DateTime:yyyy-MM-dd} to {fundHistory.Prices[^1].DateTime:yyyy-MM-dd}.");
            }

            Quote? missingFundHistory;

            if (fundHistory == null)
            {
                Console.WriteLine($"{ticker}: Download entire history.");

                try
                {
                    missingFundHistory = await GetEntireHistory(ticker);
                }
                catch (Exception ex)
                {
                    // API will sometimes return 404 if ticker is being updated on their end
                    Console.WriteLine($"{ticker}: ERROR DOWNLOAD FAILED: {ex.Message}");
                    return false;
                }
            }
            else
            {
                try
                {
                    missingFundHistory = await GetMissingHistory(fundHistory, out DateTime missingStart, out DateTime missingEnd);

                    if (missingFundHistory != null)
                    {
                        Console.WriteLine($"{ticker}: Missing history identified as {missingStart:yyyy-MM-dd} to {missingEnd:yyyy-MM-dd}");
                    }
                }
                catch (Exception ex)
                {
                    // API will sometimes return 404 if ticker is being updated on their end
                    Console.WriteLine($"{ticker}: ERROR DOWNLOAD FAILED: {ex.Message}");
                    return false;
                }
            }

            if (missingFundHistory == null)
            {
                Console.WriteLine($"{ticker}: No history missing.");
                return true;
            }

            Console.WriteLine($"{ticker}: Save {missingFundHistory.Prices.Count} new record(s), {missingFundHistory.Prices[0].DateTime:yyyy-MM-dd} to {missingFundHistory.Prices[^1].DateTime:yyyy-MM-dd}.");

            await quotesCache.Append(missingFundHistory);

            return true;
        }

        public static async Task RefreshQuotes(QuoteRepository quotesCache, HashSet<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(quotesCache);
            ArgumentNullException.ThrowIfNull(tickers);

            foreach (var ticker in tickers)
            {
                await RefreshQuote(quotesCache, ticker);
                Console.WriteLine();
            }
        }

        private static Task<Quote?> GetMissingHistory(Quote history, out DateTime start, out DateTime end)
        {
            ArgumentNullException.ThrowIfNull(history);

            var lastDate = history.Prices[^1].DateTime.Date;

            start = lastDate.AddDays(1);
            end = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);

            if (!(start < end))
            {
                return Task.FromResult<Quote?>(null);
            }

            return GetHistoryRange(history.Ticker, start, end);
        }

        private static Task<Quote?> GetEntireHistory(string ticker)
        {
            return GetHistoryRange(ticker, new DateTime(1900, 1, 1), DateTime.UtcNow.Date.AddDays(-1));
        }

        private static async Task<Quote?> GetHistoryRange(string ticker, DateTime start, DateTime end)
        {
            if (start >= end)
            {
                return null;
            }

            var history = await GetQuote(ticker, start, end);

            if (history.Prices.Count == 1 && history.Prices[0].DateTime == start.AddDays(-1))
            {
                return null;
            }

            // API will sometimes return zeroed records if the record's date is today and markets are open

            if (history.Prices[^1].Open == 0)
            {
                history.Prices.RemoveAt(history.Prices.Count - 1);
            }

            if (history.Splits.Count > 0 && (history.Splits[^1].BeforeSplit == 0 || history.Splits[^1].AfterSplit == 0))
            {
                history.Splits.RemoveAt(history.Splits.Count - 1);
            }

            if (history.Dividends.Count > 0 && history.Dividends[^1].Dividend == 0)
            {
                history.Dividends.RemoveAt(history.Dividends.Count - 1);
            }

            return history;
        }

        private static async Task<Quote> GetQuote(string ticker, DateTime start, DateTime end, bool useLibraryA = true)
        {
            static async Task<T> throttle<T>(Func<Task<T>> operation)
            {
                await Task.Delay(2500);
                return await operation();
            }

            if (useLibraryA)
            {
                var dividends = await throttle(() => YahooFinanceApi.Yahoo.GetDividendsAsync(ticker, start, end));
                var prices = await throttle(() => YahooFinanceApi.Yahoo.GetHistoricalAsync(ticker, start, end));
                var splits = await throttle(() => YahooFinanceApi.Yahoo.GetSplitsAsync(ticker, start, end));

                return new Quote(ticker)
                {
                    Dividends = dividends.Select(div => new QuoteDividend(div)).ToList(),
                    Prices = prices.Select(price => new QuotePrice(price)).ToList(),
                    Splits = splits.Select(split => new QuoteSplit(split)).ToList()
                };
            }

            var yahooQuotes = new YahooQuotesApi.YahooQuotesBuilder()
                .WithHistoryStartDate(NodaTime.Instant.FromUtc(start.Year, start.Month, start.Day, 0, 0))
                .WithCacheDuration(NodaTime.Duration.FromMinutes(1), NodaTime.Duration.FromMinutes(1))
                .WithPriceHistoryFrequency(YahooQuotesApi.Frequency.Daily)
                .Build();

            var security = await throttle(() => yahooQuotes.GetAsync(ticker, YahooQuotesApi.Histories.All))
                ?? throw new ArgumentException($"Unknown symbol '{ticker}'");

            return new Quote(ticker)
            {
                Dividends = security.DividendHistory.Value.Select(div => new QuoteDividend(div)).ToList(),
                Prices = security.PriceHistory.Value.Select(price => new QuotePrice(price)).ToList(),
                Splits = security.SplitHistory.Value.Select(split => new QuoteSplit(split)).ToList()
            };
        }
    }
}
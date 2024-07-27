using YahooQuotesApi;
using FundHistoryCache.Models;

namespace FundHistoryCache.Extensions
{
    public static class YahooQuotesBuilderExtensions
    {
        private static async Task<T> Throttle<T>(Func<Task<T>> operation)
        {
            await Task.Delay(2500);
            return await operation();
        }

        private static async Task<List<TQuote>> GetHistory<TQuote, THistory>(
            this YahooQuotes yahooQuotes,
            string ticker,
            Histories historyType,
            Func<Security, IEnumerable<THistory>> getHistory,
            Func<THistory, TQuote> createQuote)
        {
            var security = await Throttle(() => yahooQuotes.GetAsync(ticker, historyType))
                ?? throw new ArgumentException($"Unknown symbol '{ticker}'");

            return getHistory(security).Select(createQuote).ToList();
        }

        public static Task<List<QuoteDividend>> GetDividends(this YahooQuotes yahooQuotes, string ticker) =>
            yahooQuotes.GetHistory<QuoteDividend, DividendTick>(
                ticker,
                Histories.DividendHistory,
                s => s.DividendHistory.Value,
                d => new QuoteDividend(d));

        public static Task<List<QuotePrice>> GetPrices(this YahooQuotes yahooQuotes, string ticker) =>
            yahooQuotes.GetHistory<QuotePrice, PriceTick>(
                ticker,
                Histories.PriceHistory,
                s => s.PriceHistory.Value,
                p => new QuotePrice(p));

        public static Task<List<QuoteSplit>> GetSplits(this YahooQuotes yahooQuotes, string ticker) =>
            yahooQuotes.GetHistory<QuoteSplit, SplitTick>(
                ticker,
                Histories.SplitHistory,
                s => s.SplitHistory.Value,
                s => new QuoteSplit(s));
    }
}
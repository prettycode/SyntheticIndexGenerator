using Data.Models;
using NodaTime;
using YahooQuotesApi;

namespace Data.QuoteProvider
{
    public class YahooQuotesApiQuoteProvider : QuoteProvider, IQuoteProvider
    {
        public async Task<Quote?> GetQuote(
            string ticker,
            DateTime? startDate,
            DateTime? endDate)
        {
            var utcStartDate = (startDate ?? DateTime.MinValue).ToUniversalTime();

            if (endDate.HasValue)
            {
                throw new NotImplementedException($"Non-null `{nameof(endDate)}` not supported.");
            }

            var yahooQuotes = new YahooQuotesBuilder()
                .WithHistoryStartDate(Instant.FromUtc(utcStartDate.Year, utcStartDate.Month, utcStartDate.Day, 0, 0))
                .WithCacheDuration(Duration.FromHours(1), Duration.FromHours(1))
                .WithPriceHistoryFrequency(Frequency.Daily)
                .Build();

            var allHistory = await Throttle(() => yahooQuotes.GetAsync(ticker, Histories.All))
                ?? throw new InvalidOperationException($"Could not fetch history for '{ticker}'.");

            var divs = allHistory.DividendHistory.Value.Select(div => new QuoteDividend(div)).ToList();
            var prices = allHistory.PriceHistory.Value.Select(price => new QuotePrice(price)).ToList();
            var splits = allHistory.SplitHistory.Value.Select(split => new QuoteSplit(split)).ToList();

            return base.GetQuote(ticker, divs, prices, splits);
        }
    }
}

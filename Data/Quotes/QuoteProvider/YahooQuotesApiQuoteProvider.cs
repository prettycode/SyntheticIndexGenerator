using Data.Quotes.Extensions;
using NodaTime;
using YahooQuotesApi;

namespace Data.Quotes.QuoteProvider;

public class YahooQuotesApiQuoteProvider : YahooQuoteProvider, IQuoteProvider
{
    public bool RunGetQuoteSingleThreaded => true;

    public async Task<Quote?> GetQuote(
        string ticker,
        DateTime? startDate,
        DateTime? endDate)
    {
        var utcStartDate = (startDate ?? new DateTime(1927, 1, 1)).ToUniversalTime();

        if (endDate.HasValue)
        {
            throw new NotImplementedException($"Non-null `{nameof(endDate)}` not supported.");
        }

        var yahooQuotes = new YahooQuotesBuilder()
            .WithHistoryStartDate(Instant.FromUtc(utcStartDate.Year, utcStartDate.Month, utcStartDate.Day, 0, 0))
            .WithCacheDuration(Duration.FromHours(1), Duration.FromHours(1))
            .WithPriceHistoryFrequency(Frequency.Daily)
            .Build();

        var allHistory = await yahooQuotes.GetAsync(ticker, Histories.PriceHistory)
            ?? throw new InvalidOperationException($"Could not fetch history for '{ticker}'.");

        var prices = from priceTicker in allHistory.PriceHistory.Value
                     select new QuotePrice()
                     {
                         Ticker = ticker,
                         DateTime = priceTicker.Date.ToDateTimeUnspecified(),
                         Open = Convert.ToDecimal(priceTicker.Open).ToQuotePrice(),
                         High = Convert.ToDecimal(priceTicker.High).ToQuotePrice(),
                         Low = Convert.ToDecimal(priceTicker.Low).ToQuotePrice(),
                         Close = Convert.ToDecimal(priceTicker.Close).ToQuotePrice(),
                         AdjustedClose = Convert.ToDecimal(priceTicker.AdjustedClose).ToQuotePrice(),
                         Volume = priceTicker.Volume
                     };

        return GetQuote(ticker, [], prices.ToList(), []);
    }
}

using Data.Quotes.Extensions;
using YahooFinanceApi;

namespace Data.Quotes.QuoteProvider;

public class YahooFinanceApiQuoteProvider : YahooQuoteProvider, IQuoteProvider
{

    public bool RunGetQuoteSingleThreaded => true;

    public async Task<Quote?> GetQuote(string ticker, DateTime? startDate, DateTime? endDate)
    {
        // Yahoo! Finance goes back to 1927 for S&P 500 (^GSPC)
        startDate ??= new DateTime(1927, 1, 1);

        var prices = from candle in await Yahoo.GetHistoricalAsync(ticker, startDate, endDate)
                     select new QuotePrice()
                     {
                         Ticker = ticker,
                         DateTime = candle.DateTime,
                         Open = candle.Open.ToQuotePrice(),
                         High = candle.High.ToQuotePrice(),
                         Low = candle.Low.ToQuotePrice(),
                         Close = candle.Close.ToQuotePrice(),
                         AdjustedClose = candle.AdjustedClose.ToQuotePrice(),
                         Volume = candle.Volume
                     };

        return GetQuote(ticker, [], prices.ToList(), []);
    }
}

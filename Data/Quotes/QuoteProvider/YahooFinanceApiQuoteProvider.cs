namespace Data.Quotes.QuoteProvider;

public class YahooFinanceApiQuoteProvider : QuoteProvider, IQuoteProvider
{
    public async Task<Quote?> GetQuote(
        string ticker,
        DateTime? startDate,
        DateTime? endDate)
    {
        // Yahoo! Finance goes back to 1927 for S&P 500 (^GSPC)
        startDate ??= new DateTime(1927, 1, 1);

        var libDivs = await YahooFinanceApi.Yahoo.GetDividendsAsync(ticker, startDate, endDate);
        var libPrices = await YahooFinanceApi.Yahoo.GetHistoricalAsync(ticker, startDate, endDate);
        var libSplits = await YahooFinanceApi.Yahoo.GetSplitsAsync(ticker, startDate, endDate);

        var divs = libDivs.Select(div => new QuoteDividend(ticker, div)).ToList();
        var prices = libPrices.Select(price => new QuotePrice(ticker, price)).ToList();
        var splits = libSplits.Select(split => new QuoteSplit(ticker, split)).ToList();

        return GetQuote(ticker, divs, prices, splits);
    }
}

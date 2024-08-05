using Data.Models;

namespace Data.QuoteProvider
{
    public class YahooFinanceApiQuoteProvider : QuoteProvider, IQuoteProvider
    {
        public async Task<Quote?> GetQuote(
            string ticker,
            DateTime? startDate,
            DateTime? endDate)
        {
            var libDivs = await Throttle(() => YahooFinanceApi.Yahoo.GetDividendsAsync(ticker, startDate, endDate));
            var libPrices = await Throttle(() => YahooFinanceApi.Yahoo.GetHistoricalAsync(ticker, startDate, endDate));
            var libSplits = await Throttle(() => YahooFinanceApi.Yahoo.GetSplitsAsync(ticker, startDate, endDate));

            var divs = libDivs.Select(div => new QuoteDividend(div)).ToList();
            var prices = libPrices.Select(price => new QuotePrice(price)).ToList();
            var splits = libSplits.Select(split => new QuoteSplit(split)).ToList();

            return base.GetQuote(ticker, divs, prices, splits);
        }
    }
}

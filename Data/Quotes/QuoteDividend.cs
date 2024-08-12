using Data.Quotes.Extensions;

namespace Data.Quotes
{
    public readonly struct QuoteDividend
    {
        public string Ticker { get; init; }

        public DateTime DateTime { get; init; }

        public decimal Dividend { get; init; }

        public QuoteDividend(string ticker, YahooQuotesApi.DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            Ticker = ticker;
            DateTime = dividend.Date.ToDateTimeUnspecified();
            Dividend = Convert.ToDecimal(dividend.Dividend).ToQuotePrice();
        }

        public QuoteDividend(string ticker, YahooFinanceApi.DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            Ticker = ticker;
            DateTime = dividend.DateTime;
            Dividend = dividend.Dividend.ToQuotePrice();
        }
    }
}
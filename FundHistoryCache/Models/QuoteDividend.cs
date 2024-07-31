using FundHistoryCache.Extensions;

namespace FundHistoryCache.Models
{
    public readonly struct QuoteDividend
    {
        public DateTime DateTime { get; init; }

        public decimal Dividend { get; init; }

        public QuoteDividend(YahooQuotesApi.DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            DateTime = dividend.Date.ToDateTimeUnspecified();
            Dividend = Convert.ToDecimal(dividend.Dividend).ToQuotePrice();
        }

        public QuoteDividend(YahooFinanceApi.DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            DateTime = dividend.DateTime;
            Dividend = dividend.Dividend.ToQuotePrice();
        }
    }
}
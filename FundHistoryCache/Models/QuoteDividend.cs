using FundHistoryCache.Extensions;

namespace FundHistoryCache.Models
{
    public struct QuoteDividend
    {
        public DateTime DateTime { get; set; }

        public decimal Dividend { get; set; }

        public QuoteDividend() { }

        public QuoteDividend(YahooQuotesApi.DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            DateTime = dividend.Date.ToDateTimeUnspecified();
            Dividend = Convert.ToDecimal(dividend.Dividend);
        }

        public QuoteDividend(YahooFinanceApi.DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            DateTime = dividend.DateTime;
            Dividend = dividend.Dividend.TrimZeros();
        }
    }
}
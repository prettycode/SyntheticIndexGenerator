using YahooFinanceApi;

namespace FundHistoryCache.Models
{
    public struct QuoteDividendRecord
    {
        public DateTime DateTime { get; set; }

        public decimal Dividend { get; set; }

        public QuoteDividendRecord() { }

        public QuoteDividendRecord(DividendTick dividend)
        {
            ArgumentNullException.ThrowIfNull(dividend);

            DateTime = dividend.DateTime;
            Dividend = dividend.Dividend;
        }
    }
}
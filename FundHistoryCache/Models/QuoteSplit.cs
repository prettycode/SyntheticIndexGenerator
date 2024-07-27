using YahooQuotesApi;
using Legacy = YahooFinanceApi;

namespace FundHistoryCache.Models
{
    public struct QuoteSplit
    {
        public DateTime DateTime { get; set; }

        public decimal BeforeSplit { get; set; }

        public decimal AfterSplit { get; set; }

        public QuoteSplit() { }

        public QuoteSplit(SplitTick split)
        {
            ArgumentNullException.ThrowIfNull(split);

            DateTime = split.Date.ToDateTimeUnspecified();
            BeforeSplit = Convert.ToDecimal(split.BeforeSplit);
            AfterSplit = Convert.ToDecimal(split.AfterSplit);
        }

        public QuoteSplit(Legacy.SplitTick split)
        {
            ArgumentNullException.ThrowIfNull(split);

            DateTime = split.DateTime;
            BeforeSplit = split.BeforeSplit;
            AfterSplit = split.AfterSplit;
        }
    }
}
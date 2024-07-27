using YahooQuotesApi;

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
    }
}
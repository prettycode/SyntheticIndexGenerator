using YahooFinanceApi;

namespace FundHistoryCache.Models
{
    public struct QuoteSplitRecord
    {
        public DateTime DateTime { get; set; }

        public decimal BeforeSplit { get; set; }

        public decimal AfterSplit { get; set; }

        public QuoteSplitRecord() { }

        public QuoteSplitRecord(SplitTick split)
        {
            ArgumentNullException.ThrowIfNull(split);

            DateTime = split.DateTime;
            BeforeSplit = split.BeforeSplit;
            AfterSplit = split.AfterSplit;
        }
    }
}
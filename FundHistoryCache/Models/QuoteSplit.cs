namespace FundHistoryCache.Models
{
    public readonly struct QuoteSplit
    {
        public DateTime DateTime { get; init; }

        public decimal BeforeSplit { get; init; }

        public decimal AfterSplit { get; init; }

        public QuoteSplit(YahooQuotesApi.SplitTick split)
        {
            ArgumentNullException.ThrowIfNull(split);

            DateTime = split.Date.ToDateTimeUnspecified();
            BeforeSplit = Convert.ToDecimal(split.BeforeSplit);
            AfterSplit = Convert.ToDecimal(split.AfterSplit);
        }

        public QuoteSplit(YahooFinanceApi.SplitTick split)
        {
            ArgumentNullException.ThrowIfNull(split);

            DateTime = split.DateTime;

            // YahooFinanceApi has the split fields reversed, so fix it here
            BeforeSplit = split.AfterSplit;
            AfterSplit = split.BeforeSplit;
        }
    }
}
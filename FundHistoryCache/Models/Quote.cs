namespace FundHistoryCache.Models
{
    public class Quote
    {
        public readonly string Ticker;
        public List<QuoteDividend> Dividends { get; set; } = [];
        public List<QuotePrice> Prices { get; set; } = [];
        public List<QuoteSplit> Splits { get; set; } = [];

        public Quote(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            Ticker = ticker;
        }

    }
}
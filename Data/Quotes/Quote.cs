namespace Data.Quotes
{
    public class Quote
    {
        public string Ticker { get; init; }

        public List<QuoteDividend> Dividends { get; init; } = [];

        public List<QuotePrice> Prices { get; init; } = [];

        public List<QuoteSplit> Splits { get; init; } = [];

        public Quote(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            Ticker = ticker;
        }

    }
}
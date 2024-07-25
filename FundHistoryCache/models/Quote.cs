public class Quote
{
    public readonly string Ticker;
    public IReadOnlyList<QuoteDividendRecord> Dividends { get; set; } = [];
    public IReadOnlyList<QuotePriceRecord> Prices { get; set; } = [];
    public IReadOnlyList<QuoteSplitRecord> Splits { get; set; } = [];

    public Quote(string ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        this.Ticker = ticker;
    }

}
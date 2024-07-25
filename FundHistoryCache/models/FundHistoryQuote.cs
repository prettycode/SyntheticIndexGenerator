public class FundHistoryQuote
{
    public readonly string Ticker;
    public IReadOnlyList<FundHistoryQuoteDividendRecord> Dividends { get; set; } = [];
    public IReadOnlyList<FundHistoryQuotePriceRecord> Prices { get; set; } = [];
    public IReadOnlyList<FundHistoryQuoteSplitRecord> Splits { get; set; } = [];

    public FundHistoryQuote(string ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        this.Ticker = ticker;
    }

}
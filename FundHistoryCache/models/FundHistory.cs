public class FundHistory
{
    public readonly string Ticker;
    public IReadOnlyList<DividendRecord> Dividends { get; set; } = [];
    public IReadOnlyList<PriceRecord> Prices { get; set; } = [];
    public IReadOnlyList<SplitRecord> Splits { get; set; } = [];

    public FundHistory(string ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        this.Ticker = ticker;
    }

}
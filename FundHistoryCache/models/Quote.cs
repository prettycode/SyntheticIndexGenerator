using FundHistoryCache.Models;

public class Quote
{
    public readonly string Ticker;
    public List<QuoteDividendRecord> Dividends { get; set; } = [];
    public List<QuotePriceRecord> Prices { get; set; } = [];
    public List<QuoteSplitRecord> Splits { get; set; } = [];

    public Quote(string ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        this.Ticker = ticker;
    }

}
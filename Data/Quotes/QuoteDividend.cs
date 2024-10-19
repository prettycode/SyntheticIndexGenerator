namespace Data.Quotes;

public readonly struct QuoteDividend
{
    public string Ticker { get; init; }

    public DateTime DateTime { get; init; }

    public decimal Dividend { get; init; }
}
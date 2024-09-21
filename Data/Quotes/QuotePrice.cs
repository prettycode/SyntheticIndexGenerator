namespace Data.Quotes;

public readonly struct QuotePrice
{
    public string Ticker { get; init; }

    public DateTime DateTime { get; init; }

    public decimal Open { get; init; }

    public decimal High { get; init; }

    public decimal Low { get; init; }

    public decimal Close { get; init; }

    public long Volume { get; init; }

    public decimal AdjustedClose { get; init; }
}
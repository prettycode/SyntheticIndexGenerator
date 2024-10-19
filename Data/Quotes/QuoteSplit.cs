namespace Data.Quotes;

public readonly struct QuoteSplit
{
    public string Ticker { get; init; }

    public DateTime DateTime { get; init; }

    public decimal BeforeSplit { get; init; }

    public decimal AfterSplit { get; init; }
}
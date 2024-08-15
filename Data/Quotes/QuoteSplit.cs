namespace Data.Quotes;

public readonly struct QuoteSplit
{
    public string Ticker { get; init; }

    public DateTime DateTime { get; init; }

    public decimal BeforeSplit { get; init; }

    public decimal AfterSplit { get; init; }

    public QuoteSplit(string ticker, YahooQuotesApi.SplitTick split)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(split);

        Ticker = ticker;
        DateTime = split.Date.ToDateTimeUnspecified();
        BeforeSplit = Convert.ToDecimal(split.BeforeSplit);
        AfterSplit = Convert.ToDecimal(split.AfterSplit);
    }

    public QuoteSplit(string ticker, YahooFinanceApi.SplitTick split)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(split);

        Ticker = ticker;
        DateTime = split.DateTime;

        // YahooFinanceApi has the split fields reversed, so fix it here
        BeforeSplit = split.AfterSplit;
        AfterSplit = split.BeforeSplit;
    }
}
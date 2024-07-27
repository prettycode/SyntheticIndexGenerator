using YahooFinanceApi;

public struct QuoteSplitRecord
{
    public DateTime DateTime { get; set; }

    public decimal BeforeSplit { get; set; }

    public decimal AfterSplit { get; set; }

    public QuoteSplitRecord() { }

    public QuoteSplitRecord(SplitTick split)
    {
        ArgumentNullException.ThrowIfNull(split);

        this.DateTime = split.DateTime;
        this.BeforeSplit = split.BeforeSplit;
        this.AfterSplit = split.AfterSplit;
    }
}
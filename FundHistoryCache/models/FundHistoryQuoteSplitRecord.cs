using YahooFinanceApi;
public struct FundHistoryQuoteSplitRecord
{
    public DateTime DateTime { get; set; }

    public decimal BeforeSplit { get; set; }

    public decimal AfterSplit { get; set; }

    public FundHistoryQuoteSplitRecord() { }

    public FundHistoryQuoteSplitRecord(SplitTick split)
    {
        ArgumentNullException.ThrowIfNull(split);

        this.DateTime = split.DateTime;
        this.BeforeSplit = split.BeforeSplit;
        this.AfterSplit = split.AfterSplit;
    }
}
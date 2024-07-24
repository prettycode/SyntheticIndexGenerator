using YahooFinanceApi;

public struct DividendRecord
{
    public DateTime DateTime { get; set; }

    public decimal Dividend { get; set; }

    public DividendRecord() { }

    public DividendRecord(DividendTick dividend)
    {
        ArgumentNullException.ThrowIfNull(dividend);

        this.DateTime = dividend.DateTime;
        this.Dividend = dividend.Dividend;
    }
}
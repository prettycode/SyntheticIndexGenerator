using YahooFinanceApi;

public struct QuoteDividendRecord
{
    public DateTime DateTime { get; set; }

    public decimal Dividend { get; set; }

    public QuoteDividendRecord() { }

    public QuoteDividendRecord(DividendTick dividend)
    {
        ArgumentNullException.ThrowIfNull(dividend);

        this.DateTime = dividend.DateTime;
        this.Dividend = dividend.Dividend;
    }
}
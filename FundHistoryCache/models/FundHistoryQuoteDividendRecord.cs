using YahooFinanceApi;

public struct FundHistoryQuoteDividendRecord
{
    public DateTime DateTime { get; set; }

    public decimal Dividend { get; set; }

    public FundHistoryQuoteDividendRecord() { }

    public FundHistoryQuoteDividendRecord(DividendTick dividend)
    {
        ArgumentNullException.ThrowIfNull(dividend);

        this.DateTime = dividend.DateTime;
        this.Dividend = dividend.Dividend;
    }
}
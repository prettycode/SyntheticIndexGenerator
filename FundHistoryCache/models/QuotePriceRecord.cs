using YahooFinanceApi;

public struct QuotePriceRecord
{
    public DateTime DateTime { get; set; }

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public long Volume { get; set; }

    public decimal AdjustedClose { get; set; }

    public QuotePriceRecord() { }

    public QuotePriceRecord(Candle candle)
    {
        ArgumentNullException.ThrowIfNull(candle);

        this.DateTime = candle.DateTime;
        this.Open = candle.Open;
        this.High = candle.High;
        this.Low = candle.Low;
        this.Close = candle.Close;
        this.Volume = candle.Volume;
        this.AdjustedClose = candle.AdjustedClose;
    }
}
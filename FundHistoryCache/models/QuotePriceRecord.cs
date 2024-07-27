using YahooFinanceApi;

namespace FundHistoryCache.Models
{
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

            DateTime = candle.DateTime;
            Open = candle.Open;
            High = candle.High;
            Low = candle.Low;
            Close = candle.Close;
            Volume = candle.Volume;
            AdjustedClose = candle.AdjustedClose;
        }
    }
}
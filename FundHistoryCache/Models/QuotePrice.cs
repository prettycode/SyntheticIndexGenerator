using FundHistoryCache.Extensions;

namespace FundHistoryCache.Models
{
    public struct QuotePrice
    {
        public DateTime DateTime { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }

        public long Volume { get; set; }

        public decimal AdjustedClose { get; set; }

        public QuotePrice() { }

        public QuotePrice(YahooQuotesApi.PriceTick price)
        {
            ArgumentNullException.ThrowIfNull(price);

            DateTime = price.Date.ToDateTimeUnspecified();
            Open = Convert.ToDecimal(price.Open).ToQuotePrice();
            High = Convert.ToDecimal(price.High).ToQuotePrice();
            Low = Convert.ToDecimal(price.Low).ToQuotePrice();
            Close = Convert.ToDecimal(price.Close).ToQuotePrice();
            AdjustedClose = Convert.ToDecimal(price.AdjustedClose).ToQuotePrice();
            Volume = price.Volume;
        }

        public QuotePrice(YahooFinanceApi.Candle candle)
        {
            ArgumentNullException.ThrowIfNull(candle);

            DateTime = candle.DateTime;
            Open = candle.Open.ToQuotePrice();
            High = candle.High.ToQuotePrice();
            Low = candle.Low.ToQuotePrice();
            Close = candle.Close.ToQuotePrice();
            AdjustedClose = candle.AdjustedClose.ToQuotePrice();
            Volume = candle.Volume;
        }
    }
}
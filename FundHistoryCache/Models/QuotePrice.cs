using FundHistoryCache.Extensions;

namespace FundHistoryCache.Models
{
    public readonly struct QuotePrice
    {
        public DateTime DateTime { get; init; }

        public decimal Open { get; init; }

        public decimal High { get; init; }

        public decimal Low { get; init; }

        public decimal Close { get; init; }

        public long Volume { get; init; }

        public decimal AdjustedClose { get; init; }

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
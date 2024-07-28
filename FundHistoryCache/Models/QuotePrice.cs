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
            Open = Convert.ToDecimal(price.Open);
            High = Convert.ToDecimal(price.High);
            Low = Convert.ToDecimal(price.Low);
            Close = Convert.ToDecimal(price.Close);
            AdjustedClose = Convert.ToDecimal(price.AdjustedClose);
            Volume = price.Volume;
        }

        public QuotePrice(YahooFinanceApi.Candle candle)
        {
            // TODO
            static decimal TemporaryHack(decimal value, int sigFigs = 4, bool fillCents = false)
            {
                var rounded = Decimal.Round(value, sigFigs).ToString().TrimEnd('0');
                
                if (rounded.EndsWith('.'))
                {
                    if (fillCents)
                    {
                        rounded += "00";
                    }
                    else
                    {
                        rounded = rounded.TrimEnd('.');
                    }
                }
                else if (rounded.Length > 2 && rounded[^2] == '.')
                {
                    if (fillCents)
                    {
                        rounded += "0";
                    }
                }

                return decimal.Parse(rounded);
            }

            ArgumentNullException.ThrowIfNull(candle);

            DateTime = candle.DateTime;
            Open = TemporaryHack(candle.Open);
            High = TemporaryHack(candle.High);
            Low = TemporaryHack(candle.Low);
            Close = TemporaryHack(candle.Close);
            AdjustedClose = TemporaryHack(candle.AdjustedClose, 5);
            Volume = candle.Volume;
        }
    }
}
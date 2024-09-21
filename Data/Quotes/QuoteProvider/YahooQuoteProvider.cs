namespace Data.Quotes.QuoteProvider;

public abstract class YahooQuoteProvider
{
    protected static Quote? GetQuote(string ticker, List<QuoteDividend> dividends, List<QuotePrice> prices, List<QuoteSplit> splits)
    {
        // API sometimes returns a record with 0s when record is today and not yet updated after market close.
        // Other times it returns a candle with data representing the current daily performance. Discard either.

        if (prices[^1].Open == 0 ||
            prices[^1].DateTime == DateTime.Today)
        {
            var incompleteDate = prices[^1].DateTime;

            prices.RemoveAt(prices.Count - 1);

            if (prices.Count == 0)
            {
                return null;
            }

            if (dividends.Count > 0 &&
                dividends[^1].DateTime == incompleteDate)
            {
                dividends.RemoveAt(dividends.Count - 1);
            }

            if (splits.Count > 0 &&
                splits[^1].DateTime == incompleteDate)
            {
                splits.RemoveAt(splits.Count - 1);
            }
        }

        return new Quote(ticker)
        {
            Dividends = dividends,
            Prices = prices,
            Splits = splits
        };
    }
}

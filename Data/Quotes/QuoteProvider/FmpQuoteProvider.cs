using System.Text.Json;
using System.Web;
using Data.Quotes.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Data.Quotes.QuoteProvider;

public class FmpQuoteProvider(ILogger<FmpQuoteProvider> logger, IOptions<FmpQuoteProviderOptions> options) : IQuoteProvider
{
    private class QuotePriceResponse
    {
        public required string Symbol { get; set; }
        public required IEnumerable<FmpQuotePrice> Historical { get; set; }
    }

    private class FmpQuotePrice
    {
        public string Date { get; set; } = string.Empty;
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal AdjClose { get; set; }
        public long Volume { get; set; }
        public long UnadjustedVolume { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public decimal? Vwap { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal ChangeOverTime { get; set; }
    }

    private readonly Uri BaseUri = new("https://financialmodelingprep.com/api/v3/historical-price-full/");

    private readonly string ApiKey = options.Value.ApiKey;

    private readonly JsonSerializerOptions deserializeJsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private async Task<IEnumerable<FmpQuotePrice>> GetQuotePrices(string symbol, DateTime? fromDate = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }

        var uriBuilder = new UriBuilder(new Uri(BaseUri, HttpUtility.UrlEncode(symbol)));
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["apikey"] = ApiKey;

        if (fromDate.HasValue)
        {
            query["from"] = $"{fromDate.Value:yyyy-MM-dd}";
        }
        else
        {
            query["from"] = $"{DateTime.MinValue:yyyy-MM-dd}";
        }

        uriBuilder.Query = query.ToString();

        Uri requestUri = uriBuilder.Uri;
        using HttpClient httpClient = new();
        string jsonResponse = await httpClient.GetStringAsync(requestUri);

        try
        {
            var quotePriceResponse = JsonSerializer.Deserialize<QuotePriceResponse>(jsonResponse, deserializeJsonOptions);

            // API returns historical data in DESC order, so reverse it

            return quotePriceResponse?.Historical?.Reverse()
                ?? throw new InvalidOperationException("Downloaded history is unavailable or invalid.");
        }
        catch (JsonException jsonException)
        {
            // FMP returns {} for not-found symbols

            if (jsonResponse == "{}")
            {
                logger.LogError(
                    jsonException.InnerException,
                    "{ticker}: Quote provider returned '{{}}' for start date '{fromDate}' at '{requestUri}'.",
                    symbol,
                    fromDate,
                    requestUri);

                throw new KeyNotFoundException($"Quote provider could not find symbol '{symbol}'.", jsonException);
            }

            logger.LogError(
                jsonException.InnerException,
                "{ticker}: Could not parse API response from quote provider for start date '{fromDate}' at '{requestUri}'. Response: {jsonResponse}",
                symbol,
                fromDate,
                requestUri,
                jsonResponse);

            throw new KeyNotFoundException($"Quote provider returned malformed history for '{symbol}'.", jsonException);
        }
    }

    public async Task<Quote?> GetQuote(string ticker, DateTime? startDate, DateTime? endDate)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(ticker, nameof(ticker));

        if (endDate.HasValue)
        {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Must be null.");
        }

        var prices = (from price in await GetQuotePrices(ticker, startDate)
                      select new QuotePrice()
                      {
                          Ticker = ticker,
                          DateTime = DateTime.Parse(price.Date),
                          Open = Convert.ToDecimal(price.Open).ToQuotePrice(),
                          High = Convert.ToDecimal(price.High).ToQuotePrice(),
                          Low = Convert.ToDecimal(price.Low).ToQuotePrice(),
                          Close = Convert.ToDecimal(price.Close).ToQuotePrice(),
                          AdjustedClose = Convert.ToDecimal(price.AdjClose).ToQuotePrice(),
                          Volume = price.Volume
                      })
                      .ToList();

        // API returns EOD data for all historical records and current intraday quote for today's date.
        // Discard this mutable data point.

        // TODO AddDays(-1) is a dirty hack to avoid dealing with time zones. Replace with accurate adjustment.
        while (prices.Count > 0 && prices[^1].DateTime.Date >= DateTime.Now.Date.AddDays(-1))
        {
            prices.RemoveAt(prices.Count - 1);
        }

        if (prices.Count == 0)
        {
            return null;
        }

        return new Quote(ticker)
        {
            Dividends = [],
            Prices = prices,
            Splits = []
        };
    }
}
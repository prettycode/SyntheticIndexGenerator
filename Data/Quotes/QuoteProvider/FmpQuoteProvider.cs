using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Data.Quotes.QuoteProvider;

public class FmpQuoteProvider : IQuoteProvider
{
    private readonly ILogger<FmpQuoteProvider> Logger;

    private readonly Uri BaseUri = new("https://financialmodelingprep.com/api/v3/historical-price-full/");

    private readonly string ApiKey;

    public FmpQuoteProvider(ILogger<FmpQuoteProvider> logger, IOptions<FmpQuoteProviderOptions>? options = null)
    {
        ApiKey = options == null
            ? new FmpQuoteProviderOptions().ApiKey
            : options.Value.ApiKey;
        Logger = logger;
    }

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
            var quotePriceResponse = JsonSerializer.Deserialize<QuotePriceResponse>(jsonResponse);

            // API returns historical data in DESC order, so reverse it

            return quotePriceResponse?.Historical?.Reverse()
                ?? throw new InvalidOperationException("Downloaded history is unavailable or invalid.");
        }
        catch (JsonException jsonException)
        {
            // FMP returns {} for not-found symbols

            if (jsonResponse == "{}")
            {
                Logger.LogError(
                    jsonException.InnerException,
                    "{ticker}: Quote provider returned '{{}}' for start date '{fromDate}' at '{requestUri}'.",
                    symbol,
                    fromDate,
                    requestUri);

                throw new KeyNotFoundException($"Quote provider could not find symbol '{symbol}'.", jsonException);
            }

            Logger.LogError(
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

        var prices = (await GetQuotePrices(ticker, startDate)).Select(fmp => new QuotePrice(ticker, fmp)).ToList();

        // API returns EOD data for all historical records and current intraday quote for today's date.
        // Discard this mutable data point.

        // TODO AddDays(-1) is a dirty hack to avoid dealing with time zones. Replace with accurate adjustment.
        while (prices.Count > 0 && prices[^1].DateTime.Date >= DateTime.Now.Date.AddDays(-1))
        {
            prices.RemoveAt(prices.Count - 1);
        }

        if (prices.Count ==  0)
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

public class QuotePriceResponse
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; set; }

    [JsonPropertyName("historical")]
    public required IEnumerable<FmpQuotePrice> Historical { get; set; }
}

public class FmpQuotePrice
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("open")]
    public decimal Open { get; set; }

    [JsonPropertyName("high")]
    public decimal High { get; set; }

    [JsonPropertyName("low")]
    public decimal Low { get; set; }

    [JsonPropertyName("close")]
    public decimal Close { get; set; }

    [JsonPropertyName("adjClose")]
    public decimal AdjClose { get; set; }

    [JsonPropertyName("volume")]
    public long Volume { get; set; }

    [JsonPropertyName("unadjustedVolume")]
    public long UnadjustedVolume { get; set; }

    [JsonPropertyName("change")]
    public decimal Change { get; set; }

    [JsonPropertyName("changePercent")]
    public decimal ChangePercent { get; set; }

    [JsonPropertyName("vwap")]
    public decimal? Vwap { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("changeOverTime")]
    public decimal ChangeOverTime { get; set; }
}

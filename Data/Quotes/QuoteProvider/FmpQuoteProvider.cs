using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Options;

namespace Data.Quotes.QuoteProvider;

public class FmpQuoteProvider : IQuoteProvider
{
    private readonly Uri BaseUri = new("https://financialmodelingprep.com/api/v3/historical-price-full/");
    private readonly string ApiKey;

    public FmpQuoteProvider(IOptions<FmpQuoteProviderOptions>? options = null)
    {
        ApiKey = options == null
            ? new FmpQuoteProviderOptions().ApiKey
            : options.Value.ApiKey;
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

        uriBuilder.Query = query.ToString();

        using HttpClient httpClient = new();

        var response = await httpClient.GetFromJsonAsync<QuotePriceResponse>(uriBuilder.Uri);

        return response?.Historical?.Reverse()
            ?? throw new InvalidOperationException("Downloaded history is unavailable or invalid.");
    }

    public async Task<Quote?> GetQuote(string ticker, DateTime? startDate, DateTime? endDate)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(ticker, nameof(ticker));

        if (endDate.HasValue)
        {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Must be null.");
        }

        return new Quote(ticker)
        {
            Dividends = [],
            Prices = (await GetQuotePrices(ticker, startDate)).Select(fmp => new QuotePrice(ticker, fmp)).ToList(),
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
    public decimal Vwap { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("changeOverTime")]
    public decimal ChangeOverTime { get; set; }
}

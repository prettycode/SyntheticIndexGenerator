using System.Runtime.Serialization;
using System.Text.Json;
using Data.Quotes.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace Data.Quotes.QuoteProvider;

public class YahooFinanceChartProvider : YahooQuoteProvider, IQuoteProvider
{
    private class YahooFinanceResponse
    {
        public required YahooFinanceChart Chart { get; set; }
    }

    private class YahooFinanceChart
    {
        public required List<YahooFinanceChartResult> Result { get; set; }
        public required object? Error { get; set; }
    }

    private class YahooFinanceChartResult
    {
        public required YahooFinanceChartMeta Meta { get; set; }
        public required List<long> Timestamp { get; set; }
        public YahooFinanceChartEvents? Events { get; set; }
        public required YahooFinanceChartIndicators Indicators { get; set; }
    }

    private class YahooFinanceChartMeta
    {
        public required string Currency { get; set; }
        public required string Symbol { get; set; }
        public required string ExchangeName { get; set; }
        public required string FullExchangeName { get; set; }
        public required string InstrumentType { get; set; }
        public long FirstTradeDate { get; set; }
        public long RegularMarketTime { get; set; }
        public bool HasPrePostMarketData { get; set; }
        public int Gmtoffset { get; set; }
        public required string Timezone { get; set; }
        public required string ExchangeTimezoneName { get; set; }
        public decimal RegularMarketPrice { get; set; }
        public decimal? FiftyTwoWeekHigh { get; set; }
        public decimal? FiftyTwoWeekLow { get; set; }
        public decimal? RegularMarketDayHigh { get; set; }
        public decimal? RegularMarketDayLow { get; set; }
        public long RegularMarketVolume { get; set; }
        public required string LongName { get; set; }
        public required string ShortName { get; set; }
        public decimal ChartPreviousClose { get; set; }
        public int PriceHint { get; set; }
        public required YahooFinanceChartCurrentTradingPeriod CurrentTradingPeriod { get; set; }
        public required string DataGranularity { get; set; }
        public required string Range { get; set; }
        public required List<string> ValidRanges { get; set; }
    }

    private class YahooFinanceChartCurrentTradingPeriod
    {
        public required YahooFinanceChartTradingPeriod Pre { get; set; }
        public required YahooFinanceChartTradingPeriod Regular { get; set; }
        public required YahooFinanceChartTradingPeriod Post { get; set; }
    }

    private class YahooFinanceChartTradingPeriod
    {
        public required string Timezone { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public int Gmtoffset { get; set; }
    }

    private class YahooFinanceChartEvents
    {
        public required Dictionary<string, YahooFinanceChartDividend> Dividends { get; set; }
    }

    private class YahooFinanceChartDividend
    {
        public decimal Amount { get; set; }
        public long Date { get; set; }
    }

    private class YahooFinanceChartIndicators
    {
        public required List<YahooFinanceChartQuote> Quote { get; set; }
        public required List<YahooFinanceChartAdjClose> Adjclose { get; set; }
    }

    private class YahooFinanceChartQuote
    {
        public required List<long?> Volume { get; set; }
        public required List<decimal?> Close { get; set; }
        public required List<decimal?> High { get; set; }
        public required List<decimal?> Open { get; set; }
        public required List<decimal?> Low { get; set; }
    }

    private class YahooFinanceChartAdjClose
    {
        public required List<decimal?> Adjclose { get; set; }
    }

    public bool RunGetQuoteSingleThreaded => true;

    public async Task<Quote?> GetQuote(string ticker, DateTime? startDate, DateTime? endDate)
    {
        // Yahoo! Finance goes back to 1927 for S&P 500 (^GSPC)

        startDate ??= new DateTime(1927, 1, 1);
        endDate ??= DateTime.Now.AddDays(1);

        var response = await GetRequestResponse(ticker, startDate.Value, endDate.Value);
        var tickerHistory = response.Chart.Result[0];
        var prices = new List<QuotePrice>();

        for (var i = 0; i < tickerHistory.Timestamp.Count; i++)
        {
            var quote = tickerHistory.Indicators.Quote[0];
            var open = quote.Open[i];

            if (open == null && i == tickerHistory.Timestamp.Count - 1)
            {
                break;
            }

            prices.Add(new QuotePrice()
            {
                Ticker = ticker,
                DateTime = DateTimeOffset.FromUnixTimeSeconds(tickerHistory.Timestamp[i]).DateTime.Date,
                Open = open.Value.ToQuotePrice(),
                High = quote.High[i].Value.ToQuotePrice(),
                Low = quote.Low[i].Value.ToQuotePrice(),
                Close = quote.Close[i].Value.ToQuotePrice(),
                AdjustedClose = tickerHistory.Indicators.Adjclose[0].Adjclose[i].Value.ToQuotePrice(),
                Volume = quote.Volume[i].Value
            });
        }

        return GetQuote(ticker, [], prices.ToList(), []);
    }

    private static Uri GetRequestUri(string ticker, DateTime startDate, DateTime endDate)
    {
        const string baseAddress = "https://query2.finance.yahoo.com/v8/finance/chart/";
        long period1 = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
        long period2 = ((DateTimeOffset)endDate).ToUnixTimeSeconds();
        string url = QueryHelpers.AddQueryString($"{baseAddress}{ticker}", new Dictionary<string, string?>
        {
            ["period1"] = period1.ToString(),
            ["period2"] = period2.ToString(),
            ["events"] = "history",
            ["interval"] = "1d"
        });

        return new(url);
    }

    private static async Task<YahooFinanceResponse> GetRequestResponse(string ticker, DateTime startDate, DateTime endDate)
    {
        Uri requestUri = GetRequestUri(ticker, startDate, endDate);
        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT; Windows NT 6.2; en-US)");
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);

        response.EnsureSuccessStatusCode();

        string responseJsonContent = await response.Content.ReadAsStringAsync();
        JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        var deserializedResponse = JsonSerializer.Deserialize<YahooFinanceResponse>(responseJsonContent, jsonSerializerOptions) ?? throw new SerializationException("Response JSON deserialized to null object.");

        return deserializedResponse;
    }
}

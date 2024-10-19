using System.Runtime.Serialization;
using System.Text.Json;
using Data.Quotes.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace Data.Quotes.QuoteProvider;

public class YawhooQuoteProvider : YahooQuoteProvider, IQuoteProvider
{
    private class YahooFinanceResponse
    {
        public Chart Chart { get; set; }
    }

    private class Chart
    {
        public List<Result> Result { get; set; }
        public object Error { get; set; }
    }

    private class Result
    {
        public Meta Meta { get; set; }
        public List<long> Timestamp { get; set; }
        public Events Events { get; set; }
        public Indicators Indicators { get; set; }
    }

    private class Meta
    {
        public string Currency { get; set; }
        public string Symbol { get; set; }
        public string ExchangeName { get; set; }
        public string FullExchangeName { get; set; }
        public string InstrumentType { get; set; }
        public long FirstTradeDate { get; set; }
        public long RegularMarketTime { get; set; }
        public bool HasPrePostMarketData { get; set; }
        public int Gmtoffset { get; set; }
        public string Timezone { get; set; }
        public string ExchangeTimezoneName { get; set; }
        public decimal RegularMarketPrice { get; set; }
        public decimal? FiftyTwoWeekHigh { get; set; }
        public decimal? FiftyTwoWeekLow { get; set; }
        public decimal? RegularMarketDayHigh { get; set; }
        public decimal? RegularMarketDayLow { get; set; }
        public long RegularMarketVolume { get; set; }
        public string LongName { get; set; }
        public string ShortName { get; set; }
        public decimal ChartPreviousClose { get; set; }
        public int PriceHint { get; set; }
        public CurrentTradingPeriod CurrentTradingPeriod { get; set; }
        public string DataGranularity { get; set; }
        public string Range { get; set; }
        public List<string> ValidRanges { get; set; }
    }

    private class CurrentTradingPeriod
    {
        public TradingPeriod Pre { get; set; }
        public TradingPeriod Regular { get; set; }
        public TradingPeriod Post { get; set; }
    }

    private class TradingPeriod
    {
        public string Timezone { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public int Gmtoffset { get; set; }
    }

    private class Events
    {
        public Dictionary<string, Dividend> Dividends { get; set; }
    }

    private class Dividend
    {
        public decimal Amount { get; set; }
        public long Date { get; set; }
    }

    private class Indicators
    {
        public List<Quote> Quote { get; set; }
        public List<AdjClose> Adjclose { get; set; }
    }

    private class Quote
    {
        public List<long> Volume { get; set; }
        public List<decimal> Close { get; set; }
        public List<decimal> High { get; set; }
        public List<decimal> Open { get; set; }
        public List<decimal> Low { get; set; }
    }

    private class AdjClose
    {
        public List<decimal> Adjclose { get; set; }
    }

    public bool RunGetQuoteSingleThreaded => true;

    public async Task<Data.Quotes.Quote?> GetQuote(string ticker, DateTime? startDate, DateTime? endDate)
    {
        // TODO Yahoo! Finance goes back to 1927 for S&P 500 (^GSPC)

        startDate ??= new DateTime(1927, 1, 1);
        endDate ??= DateTime.Now.AddDays(1);

        var response = await GetRequestResponse(ticker, startDate.Value, endDate.Value);
        var tickerHistory = response.Chart.Result[0];
        var prices = new List<QuotePrice>();

        for (var i = 0; i < tickerHistory.Timestamp.Count; i++)
        {
            prices.Add(new QuotePrice()
            {
                Ticker = ticker,
                DateTime = DateTimeOffset.FromUnixTimeSeconds(tickerHistory.Timestamp[i]).DateTime.Date,
                Open = tickerHistory.Indicators.Quote[0].Open[i].ToQuotePrice(),
                High = tickerHistory.Indicators.Quote[0].High[i].ToQuotePrice(),
                Low = tickerHistory.Indicators.Quote[0].Low[i].ToQuotePrice(),
                Close = tickerHistory.Indicators.Quote[0].Close[i].ToQuotePrice(),
                AdjustedClose = tickerHistory.Indicators.Adjclose[0].Adjclose[i].ToQuotePrice(),
                Volume = tickerHistory.Indicators.Quote[0].Volume[i]
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

    private async Task<YahooFinanceResponse> GetRequestResponse(string ticker, DateTime startDate, DateTime endDate)
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

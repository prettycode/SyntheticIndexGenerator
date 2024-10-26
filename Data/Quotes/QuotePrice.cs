using System.Text.Json.Serialization;
using Data.JsonConverters;

namespace Data.Quotes;

public readonly struct QuotePrice
{
    public string Ticker { get; init; }

    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime DateTime { get; init; }

    public decimal AdjustedClose { get; init; }

    public long Volume { get; init; }
}
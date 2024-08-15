namespace Data.Quotes;

internal interface IQuotesService
{
    Task<Dictionary<string, IEnumerable<QuotePrice>>> GetPrices(HashSet<string> tickers, bool skipRefresh = false);
}
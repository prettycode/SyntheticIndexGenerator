namespace Data.Quotes;

internal interface IQuotesService
{
    Task<Dictionary<string, IEnumerable<QuotePrice>>> GetDailyQuoteHistory(HashSet<string> tickers, bool skipRefresh = false);
}
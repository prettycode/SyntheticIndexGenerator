namespace Data.Quotes;

internal interface IQuotesService
{
    Task<Dictionary<string, IEnumerable<QuotePrice>>> GetDailyQuoteHistory(HashSet<string> tickers, bool skipRefresh = false);

    Task<IEnumerable<QuotePrice>> GetDailyQuoteHistory(string ticker, bool skipRefresh = false);
}
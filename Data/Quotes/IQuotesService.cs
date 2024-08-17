namespace Data.Quotes;

internal interface IQuotesService
{
    Task<Dictionary<string, IEnumerable<QuotePrice>>> GetDailyQuoteHistory(HashSet<string> tickers);

    Task<IEnumerable<QuotePrice>> GetDailyQuoteHistory(string ticker);
}
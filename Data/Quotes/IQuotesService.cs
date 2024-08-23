namespace Data.Quotes;

internal interface IQuotesService
{
    Task<IEnumerable<QuotePrice>> GetDailyQuoteHistory(string ticker);
}
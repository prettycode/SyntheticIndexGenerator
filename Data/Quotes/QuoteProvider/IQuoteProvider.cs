namespace Data.Quotes.QuoteProvider
{
    public interface IQuoteProvider
    {
        Task<Quote?> GetQuote(string ticker, DateTime? startDate, DateTime? endDate);
    }
}

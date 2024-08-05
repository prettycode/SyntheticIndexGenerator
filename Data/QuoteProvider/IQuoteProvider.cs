using Data.Models;

namespace Data.QuoteProvider
{
    public interface IQuoteProvider
    {
        Task<Quote?> GetQuote(string ticker, DateTime? startDate, DateTime? endDate);
    }
}

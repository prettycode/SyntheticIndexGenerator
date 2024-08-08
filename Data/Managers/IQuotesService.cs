using Data.Models;

namespace Data.Controllers
{
    public interface IQuotesService
    {
        Task<Quote?> GetQuote(string ticker);

        Task<Dictionary<string, Quote?>> GetQuotes(HashSet<string> tickers);
    }
}
using Data.Models;

namespace Data.Controllers
{
    public interface IQuotesService
    {
        Task<IEnumerable<QuotePrice>> GetPriceHistory(string ticker);


        Task<Dictionary<string, Quote>> GetQuotes(HashSet<string> tickers);
    }
}
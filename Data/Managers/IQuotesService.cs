using Data.Models;

namespace Data.Controllers
{
    public interface IQuotesService
    {
        Task<Dictionary<string, IEnumerable<QuotePrice>>> GetPrices(HashSet<string> tickers);
    }
}
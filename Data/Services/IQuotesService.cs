using Data.Models;

namespace Data.Services
{
    internal interface IQuotesService
    {
        Task<Dictionary<string, IEnumerable<QuotePrice>>> GetPrices(HashSet<string> tickers, bool skipRefresh = false);
    }
}
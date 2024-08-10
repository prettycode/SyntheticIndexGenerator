using Data.Models;

namespace Data.Services
{
    public interface IQuotesService
    {
        Task<Dictionary<string, IEnumerable<QuotePrice>>> GetPrices(HashSet<string> tickers, bool skipRefresh = false);

        Task<Dictionary<string, Dictionary<string, IEnumerable<QuotePrice>>>> GetSyntheticPrices(
            HashSet<string> tickers,
            bool skipRefresh = false);
    }
}
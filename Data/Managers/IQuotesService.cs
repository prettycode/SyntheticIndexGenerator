using Data.Models;

namespace Data.Controllers
{
    public interface IQuotesService
    {
        /// <summary>
        /// Gets the historical price, dividend, and split data for a security.
        /// </summary>
        Task<Quote?> GetQuote(string ticker);

        Task<Dictionary<string, Quote?>> GetQuotes(HashSet<string> tickers);
    }
}
using Data.Models;

namespace Data.Repositories
{
    public interface IQuoteRepository
    {
        Task Append(Quote fundHistory);
        Task<Quote> Get(string ticker);
        IEnumerable<string> GetAllTickers();
        bool Has(string ticker);
        Task Replace(Quote fundHistory);
    }
}
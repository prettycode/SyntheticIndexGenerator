using Data.Models;

namespace Data.Repositories
{
    internal interface IQuoteRepository
    {
        Task Append(Quote fundHistory);

        Task<Quote> Get(string ticker, bool skipPastZeroVolume = false);

        IEnumerable<string> GetAllTickers();

        bool Has(string ticker);

        Task Replace(Quote fundHistory);
    }
}
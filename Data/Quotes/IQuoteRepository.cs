namespace Data.Quotes
{
    internal interface IQuoteRepository
    {
        Task<Quote> Append(Quote fundHistory);

        Task<Quote> Get(string ticker, bool skipPastZeroVolume = false);

        IEnumerable<string> GetAllTickers();

        bool Has(string ticker);

        Task<Quote> Replace(Quote fundHistory);
    }
}
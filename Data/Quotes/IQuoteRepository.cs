namespace Data.Quotes;

internal interface IQuoteRepository
{
    Task<Quote?> TryGetValue(string ticker);

    Task<Quote> Get(string ticker, bool skipPastZeroVolume = false);

    Task<Quote> Put(Quote fundHistory, bool append = false);
}
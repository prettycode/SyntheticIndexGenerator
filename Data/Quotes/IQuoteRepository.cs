namespace Data.Quotes;

internal interface IQuoteRepository
{
    Task<Quote?> TryGetMemoryCacheQuote(string ticker);

    Task<Quote?> TryGetFileCacheQuote(string ticker);

    Task<Quote> GetQuote(string ticker, bool skipPastZeroVolume = false);

    Task<Quote> PutQuote(Quote fundHistory, bool append = false);
}
namespace Data.Quotes;

internal interface IQuoteRepository
{
    bool Has(string ticker);

    Task<Quote> Get(string ticker, bool skipPastZeroVolume = false);


    Task<Quote> Append(Quote fundHistory);

    Task<Quote> Replace(Quote fundHistory);
}
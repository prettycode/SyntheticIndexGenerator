using Data.TableFileCache;
using Microsoft.Extensions.Options;

namespace Data.Quotes;

public class QuoteRepositoryOptions : IOptions<QuoteRepositoryOptions>
{
    public required TableCacheOptions TableCacheOptions { get; init; }

    QuoteRepositoryOptions IOptions<QuoteRepositoryOptions>.Value
    {
        get { return this; }
    }
}

using Microsoft.Extensions.Options;
using TableFileCache;

namespace Data.Quotes;

public class QuoteRepositoryOptions : IOptions<QuoteRepositoryOptions>
{
    public required TableFileCacheOptions TableCacheOptions { get; init; }

    QuoteRepositoryOptions IOptions<QuoteRepositoryOptions>.Value
    {
        get { return this; }
    }
}

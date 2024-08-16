using Data.TableFileCache;
using Data.TableFileCache.DaylongCache;
using Microsoft.Extensions.Options;

namespace Data.Returns;

public class ReturnRepositoryOptions : IOptions<ReturnRepositoryOptions>
{
    public required string SyntheticReturnsFilePath { get; init; }

    public required TableCacheOptions TableCacheOptions { get; init; }

    ReturnRepositoryOptions IOptions<ReturnRepositoryOptions>.Value
    {
        get { return this; }
    }
}

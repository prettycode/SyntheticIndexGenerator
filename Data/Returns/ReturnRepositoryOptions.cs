using Data.TableFileCache;
using Microsoft.Extensions.Options;

namespace Data.Returns;

public class ReturnRepositoryOptions : IOptions<ReturnRepositoryOptions>
{
    public required string SyntheticReturnsFilePath { get; init; }

    public required TableFileCacheOptions TableCacheOptions { get; init; }

    ReturnRepositoryOptions IOptions<ReturnRepositoryOptions>.Value
    {
        get { return this; }
    }
}

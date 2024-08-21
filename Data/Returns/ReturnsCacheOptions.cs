using Data.TableFileCache;
using Microsoft.Extensions.Options;

namespace Data.Returns;

public class ReturnsCacheOptions : IOptions<ReturnsCacheOptions>
{
    public required string SyntheticReturnsFilePath { get; init; }

    public required TableFileCacheOptions TableCacheOptions { get; init; }

    ReturnsCacheOptions IOptions<ReturnsCacheOptions>.Value
    {
        get { return this; }
    }
}

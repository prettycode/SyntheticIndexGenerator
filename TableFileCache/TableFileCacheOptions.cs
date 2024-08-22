using Microsoft.Extensions.Options;

namespace TableFileCache;

public class TableFileCacheOptions : IOptions<TableFileCacheOptions>
{
    public required string CacheRootPath { get; init; }

    public string? CacheNamespace { get; init; }

    public required bool CacheMissReadsFileCache { get; init; }

    public required DailyExpirationCacheOptions DailyExpirationOptions { get; init; }

    TableFileCacheOptions IOptions<TableFileCacheOptions>.Value
    {
        get { return this; }
    }
}

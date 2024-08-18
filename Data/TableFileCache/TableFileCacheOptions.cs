using Data.TableFileCache.DaylongCache;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

public class TableFileCacheOptions : IOptions<TableFileCacheOptions>
{
    public required string CacheRootPath { get; init; } = "./";

    public string? CacheNamespace { get; init; }

    public required bool PrimeCacheFromFiles { get; init; } = false;

    public required DaylongCacheOptions DaylongCacheOptions { get; init; }

    TableFileCacheOptions IOptions<TableFileCacheOptions>.Value
    {
        get { return this; }
    }
}

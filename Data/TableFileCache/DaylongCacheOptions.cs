using Data.TableFileCache.GenericMemoryCache;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

public class DaylongCacheOptions : IOptions<DaylongCacheOptions>
{
    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

    public TimeOnly TimeOfDay { get; init; } = new TimeOnly();

    public GenericMemoryCacheOptions? GenericMemoryCacheOptions { get; init; }

    DaylongCacheOptions IOptions<DaylongCacheOptions>.Value
    {
        get { return this; }
    }
}

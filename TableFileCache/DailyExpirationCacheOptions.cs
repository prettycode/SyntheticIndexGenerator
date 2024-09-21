using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TableFileCache;

public class DailyExpirationCacheOptions : IOptions<DailyExpirationCacheOptions>
{
    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

    public TimeOnly TimeOfDay { get; init; } = new TimeOnly();

    public MemoryCacheOptions? MemoryCacheOptions { get; init; }

    DailyExpirationCacheOptions IOptions<DailyExpirationCacheOptions>.Value => this;
}

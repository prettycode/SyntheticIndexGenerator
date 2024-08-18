using Data.TableFileCache.DaylongCache.GenericMemoryCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache.DaylongCache;

// TODO do not inherit from GenericMemoryCache; have it privately inside here instead
public class DaylongCache<TKey, TValue>(IOptions<DaylongCacheOptions> daylongCacheOptions)
        : GenericMemoryCache<TKey, TValue>(daylongCacheOptions.Value?.GenericMemoryCacheOptions) where TKey : notnull
{
    private readonly DaylongCacheOptions daylongCacheOptions = daylongCacheOptions.Value ?? new();

    public TValue Set(TKey key, TValue value)
        => cache.Set(key, value, GetNextExpirationDateTimeOffset());

    public new TValue? this[TKey key]
    {
        get => Get(key);
        set => Set(key, value ?? throw new ArgumentNullException(nameof(value)));
    }

    private DateTimeOffset GetNextExpirationDateTimeOffset()
    {
        var destinationTimeZone = daylongCacheOptions.TimeZone;
        var zonedDateTimeOffset = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, destinationTimeZone);
        var zonedDateTimeOffsetMidnight = zonedDateTimeOffset.Date;
        var zonedTodayAtTimeOfDay = zonedDateTimeOffsetMidnight.Add(daylongCacheOptions.TimeOfDay.ToTimeSpan());
        var nextExpiration = zonedTodayAtTimeOfDay > zonedDateTimeOffset
            ? zonedTodayAtTimeOfDay
            : zonedTodayAtTimeOfDay.AddDays(1);

        return new DateTimeOffset(nextExpiration, destinationTimeZone.GetUtcOffset(nextExpiration));
    }
}

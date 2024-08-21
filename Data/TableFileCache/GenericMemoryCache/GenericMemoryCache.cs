using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache.GenericMemoryCache;

// TODO doesn't dispose of the MemoryCache; should take IMemoryCache in ctor instead.
public class GenericMemoryCache<TKey, TValue>(IOptions<DaylongCacheOptions> daylongCacheOptions) where TKey
    : notnull
{
    protected readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    public TValue Set(TKey key, TValue value)
        => cache.Set(key, value, GetNextExpirationDateTimeOffset());

    public TValue? Get(TKey key) => cache.Get<TValue>(key);

    public TValue? this[TKey key]
    {
        get => Get(key);
        set => Set(key, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public bool TryGetValue(TKey key, out TValue? value) => cache.TryGetValue(key, out value);

    private DateTimeOffset GetNextExpirationDateTimeOffset()
    {
        var destinationTimeZone = daylongCacheOptions.Value.TimeZone;
        var zonedDateTimeOffset = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, destinationTimeZone);
        var zonedDateTimeOffsetMidnight = zonedDateTimeOffset.Date;
        var zonedTodayAtTimeOfDay = zonedDateTimeOffsetMidnight.Add(daylongCacheOptions.Value.TimeOfDay.ToTimeSpan());
        var nextExpiration = zonedTodayAtTimeOfDay > zonedDateTimeOffset
            ? zonedTodayAtTimeOfDay
            : zonedTodayAtTimeOfDay.AddDays(1);

        return new DateTimeOffset(nextExpiration, destinationTimeZone.GetUtcOffset(nextExpiration));
    }
}
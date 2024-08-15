using Data.TableFileCache.GenericMemoryCache;
using Microsoft.Extensions.Caching.Memory;

namespace Data.TableFileCache;

public class DailyAbsoluteExpiryCache<TKey, TValue>(TimeZoneInfo timeZone, TimeOnly timeOfDay)
    : GenericMemoryCache<TKey, TValue>(null, null) where TKey : notnull
{
    public override TValue Set(TKey key, TValue value)
        => base.cache.Set(key, value, GetNextExpirationDateTimeOffset());

    private DateTimeOffset GetNextExpirationDateTimeOffset()
    {
        var zonedDateTimeOffset = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        var zonedDateTimeOffsetMidnight = zonedDateTimeOffset.Date;
        var zonedTodayAtTimeOfDay = zonedDateTimeOffsetMidnight.Add(timeOfDay.ToTimeSpan());
        var nextExpiration = zonedTodayAtTimeOfDay > zonedDateTimeOffset
            ? zonedTodayAtTimeOfDay
            : zonedTodayAtTimeOfDay.AddDays(1);

        return new DateTimeOffset(nextExpiration, timeZone.GetUtcOffset(nextExpiration));
    }
}

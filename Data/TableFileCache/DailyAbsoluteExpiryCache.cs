using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.TableFileCache.GenericMemoryCache;
using Microsoft.Extensions.Caching.Memory;

namespace Data.TableFileCache
{
    public class DailyAbsoluteExpiryCache<TKey, TValue>(TimeZoneInfo timeZone, TimeOnly timeOfDay) 
        : GenericMemoryCache<TKey, TValue>(null, null) where TKey : notnull
    {
        public override TValue Set(TKey key, TValue value) => base.cache.Set(key, value, GetNextExpirationDateTimeOffset());

        private DateTimeOffset GetNextExpirationDateTimeOffset()
        {
            var timezonedDateTimeOffset = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
            var timezonedDateTimeOffsetMidnight = timezonedDateTimeOffset.Date;
            var timezonedTodayAtTimeOfDay = timezonedDateTimeOffsetMidnight.Add(timeOfDay.ToTimeSpan());
            var nextExpiration = timezonedTodayAtTimeOfDay > timezonedDateTimeOffset 
                ? timezonedTodayAtTimeOfDay 
                : timezonedTodayAtTimeOfDay.AddDays(1);

            return new DateTimeOffset(nextExpiration, timeZone.GetUtcOffset(nextExpiration));
        }
    }
}

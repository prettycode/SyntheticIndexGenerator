using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.TableFileCache.GenericMemoryCache;
using Microsoft.Extensions.Caching.Memory;

namespace Data.TableFileCache
{
    public class DailyAbsoluteExpiryCache<TKey, TValue>(Func<DateTimeOffset> getExpiryDateTimeOffset) 
        : GenericMemoryCache<TKey, TValue>(null, null) where TKey : notnull
    {
        public override TValue Set(TKey key, TValue value) => base.cache.Set(key, value, getExpiryDateTimeOffset());
    }
}

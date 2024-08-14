using Microsoft.Extensions.Caching.Memory;

namespace Data.TableFileCache;

public class GenericMemoryCache<TKey, TValue>(MemoryCacheEntryOptions entryOptions) where TKey : notnull
{
    private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
    private readonly MemoryCacheEntryOptions entryOptions = entryOptions;

    public void Set(TKey key, TValue value)
    {
        cache.Set(key, value, entryOptions);
    }

    public TValue? Get(TKey key)
    {
        return cache.Get<TValue>(key);
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        return cache.TryGetValue(key, out value);
    }

    public void Remove(TKey key)
    {
        cache.Remove(key);
    }
}


// Usage example
class Program
{
    static void Main()
    {
        // Create options for midnight expiration in New York
        var newYorkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var nowInNewYork = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, newYorkTimeZone);
        var midnightTonight = new DateTimeOffset(nowInNewYork.Date.AddDays(1), newYorkTimeZone.GetUtcOffset(nowInNewYork.DateTime));

        var midnightOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(midnightTonight);

        var cache = new GenericMemoryCache<string, string>(midnightOptions);

        cache.Set("key1", "value1");
        Console.WriteLine(cache.Get("key1"));
    }
}
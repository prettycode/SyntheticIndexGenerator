using Microsoft.Extensions.Caching.Memory;

namespace Data.TableFileCache.GenericMemoryCache;

public class GenericMemoryCache<TKey, TValue>(MemoryCacheOptions? cacheOptions, MemoryCacheEntryOptions? entryOptions)
    : IGenericMemoryCache<TKey, TValue> where TKey : notnull
{
    protected readonly IMemoryCache cache = new MemoryCache(cacheOptions ?? new MemoryCacheOptions());

    private readonly MemoryCacheEntryOptions entryOptions = entryOptions ?? new MemoryCacheEntryOptions();

    public virtual TValue Set(TKey key, TValue value) => cache.Set(key, value, entryOptions);

    public TValue? Get(TKey key) => cache.Get<TValue>(key);

    public TValue? this[TKey key]
    {
        get => Get(key);
        set => Set(key, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public bool TryGet(TKey key, out TValue? value) => cache.TryGetValue(key, out value);
}

/*
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
}*/
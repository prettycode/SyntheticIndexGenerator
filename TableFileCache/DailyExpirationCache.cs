using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TableFileCache;

public class DailyExpirationCache<TKey, TValue>(
    Func<DateTimeOffset> getAbsoluteExpiration,
    IMemoryCache? memoryCache = null,
    IOptions<MemoryCacheOptions>? memoryCacheOptions = null) where TKey : notnull
{
    protected readonly IMemoryCache cache = memoryCache
        ?? new MemoryCache(memoryCacheOptions?.Value ?? new MemoryCacheOptions());

    public TValue Set(TKey key, TValue value)
        => cache.Set(key, value, getAbsoluteExpiration());

    public TValue? Get(TKey key) => cache.Get<TValue>(key);

    public TValue? this[TKey key]
    {
        get => Get(key);
        set => Set(key, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public bool TryGetValue(TKey key, out TValue? value) => cache.TryGetValue(key, out value);
}
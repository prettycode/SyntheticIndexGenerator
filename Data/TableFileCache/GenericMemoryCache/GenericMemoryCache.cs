using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache.GenericMemoryCache;

public class GenericMemoryCache<TKey, TValue>(IOptions<GenericMemoryCacheOptions>? genericMemoryCacheOptions) where TKey
    : notnull
{
    public readonly MemoryCacheEntryOptions? MemoryCacheEntryOptions = genericMemoryCacheOptions?
        .Value
        .MemoryCacheEntryOptions;

    protected readonly IMemoryCache cache = new MemoryCache(genericMemoryCacheOptions?.Value.MemoryCacheOptions
        ?? new MemoryCacheOptions());

    public TValue Set(TKey key, TValue value)
        => cache.Set(key, value);

    public TValue Set(TKey key, TValue value, MemoryCacheEntryOptions? options)
        => cache.Set(key, value, options);

    public TValue Set(TKey key, TValue value, TimeSpan absoluteExpirationRelativeToNow)
        => cache.Set(key, value, absoluteExpirationRelativeToNow);

    public TValue Set(TKey key, TValue value, DateTimeOffset absoluteExpiration)
        => cache.Set(key, value, absoluteExpiration);

    public TValue? Get(TKey key) => cache.Get<TValue>(key);

    public TValue? this[TKey key]
    {
        get => Get(key);
        set => Set(key, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public bool TryGet(TKey key, out TValue? value) => cache.TryGetValue(key, out value);
}
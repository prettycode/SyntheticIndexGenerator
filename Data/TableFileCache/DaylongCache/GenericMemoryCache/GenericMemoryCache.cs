using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache.DaylongCache.GenericMemoryCache;

public class GenericMemoryCache<TKey, TValue>(IOptions<GenericMemoryCacheOptions>? genericMemoryCacheOptions) where TKey
    : notnull
{
    private readonly MemoryCacheEntryOptions? memoryCacheEntryOptions
        = genericMemoryCacheOptions?.Value.MemoryCacheEntryOptions ?? new MemoryCacheEntryOptions();

    protected readonly IMemoryCache cache = new MemoryCache(genericMemoryCacheOptions?.Value.MemoryCacheOptions
        ?? new MemoryCacheOptions());

    public TValue Set(TKey key, TValue value, MemoryCacheEntryOptions? options = null)
        => cache.Set(key, value, options ?? memoryCacheEntryOptions);

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
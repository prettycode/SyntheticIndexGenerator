using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

// TODO doesn't dispose of the MemoryCache; should take IMemoryCache in ctor instead.
public class GenericMemoryCache<TKey, TValue>(Func<DateTimeOffset> getAbsoluteExpiration, IOptions<MemoryCacheOptions>? memoryCacheOptions = null) where TKey
    : notnull
{
    protected readonly IMemoryCache cache = new MemoryCache(memoryCacheOptions?.Value ?? new MemoryCacheOptions());

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
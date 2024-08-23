using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TableFileCache;

public class AbsoluteExpirationCache<TKey, TValue> : IDisposable where TKey : notnull
{
    protected readonly IMemoryCache cache;

    private readonly bool shouldDisposeCache;

    private bool disposed = false;

    public AbsoluteExpirationCache(
        Func<DateTimeOffset> createAbsoluteExpiration,
        IMemoryCache? memoryCache = null,
        IOptions<MemoryCacheOptions>? memoryCacheOptions = null)
    {
        cache = memoryCache ?? new MemoryCache(memoryCacheOptions?.Value ?? new MemoryCacheOptions());
        shouldDisposeCache = memoryCache == null;
        CreateAbsoluteExpiration = createAbsoluteExpiration;
    }

    private Func<DateTimeOffset> CreateAbsoluteExpiration { get; }

    public TValue Set(TKey key, TValue value) => cache.Set(key, value, CreateAbsoluteExpiration());

    public TValue? Get(TKey key) => cache.Get<TValue>(key);

    public TValue? this[TKey key]
    {
        get => Get(key);
        set => Set(key, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public bool TryGetValue(TKey key, out TValue? value) => cache.TryGetValue(key, out value);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing && shouldDisposeCache)
            {
                cache.Dispose();
            }

            disposed = true;
        }
    }
}
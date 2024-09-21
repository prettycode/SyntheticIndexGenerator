using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TableFileCache;

public class AbsoluteExpirationCache<TKey, TValue>(
    Func<DateTimeOffset> createAbsoluteExpiration,
    IMemoryCache? memoryCache = null,
    IOptions<MemoryCacheOptions>? memoryCacheOptions = null) : IDisposable where TKey : notnull
{
    protected readonly IMemoryCache cache = memoryCache ?? new MemoryCache(memoryCacheOptions?.Value ?? new MemoryCacheOptions());

    private readonly bool shouldDisposeCache = memoryCache == null;

    private bool disposed = false;

    private Func<DateTimeOffset> CreateAbsoluteExpiration { get; } = createAbsoluteExpiration;

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
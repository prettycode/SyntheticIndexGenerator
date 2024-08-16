using Microsoft.Extensions.Caching.Memory;

namespace Data.TableFileCache.DaylongCache.GenericMemoryCache;

public interface IGenericMemoryCache<TKey, TValue> where TKey : notnull
{
    TValue? this[TKey key] { get; set; }

    TValue? Get(TKey key);

    TValue Set(TKey key, TValue value, MemoryCacheEntryOptions? options);

    TValue Set(TKey key, TValue value, DateTimeOffset absoluteExpiration);

    bool TryGet(TKey key, out TValue? value);
}
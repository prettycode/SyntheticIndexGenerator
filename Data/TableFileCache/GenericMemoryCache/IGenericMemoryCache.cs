namespace Data.TableFileCache.GenericMemoryCache
{
    public interface IGenericMemoryCache<TKey, TValue> where TKey : notnull
    {
        TValue? this[TKey key] { get; set; }

        TValue? Get(TKey key);
        TValue Set(TKey key, TValue value);
        bool TryGet(TKey key, out TValue? value);
    }
}
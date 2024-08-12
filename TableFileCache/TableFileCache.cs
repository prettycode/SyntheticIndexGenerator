using System.Collections.Concurrent;
using System.Text.Json;

namespace TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private const string CACHE_FILE_EXTENSION = "txt";
    private readonly ConcurrentDictionary<TKey, IEnumerable<TValue>> cache = new();
    private readonly string cacheRootPath;
    private readonly string cacheNamespace;
    private readonly string cacheTableName;
    private readonly string cacheTableRowKeyName;

    public TableFileCache(string cacheRootPath, string cacheNamespace, string nameofKeyProperty)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheRootPath);
        ArgumentException.ThrowIfNullOrEmpty(cacheNamespace);
        ArgumentException.ThrowIfNullOrEmpty(nameofKeyProperty);

        this.cacheRootPath = cacheRootPath;
        this.cacheNamespace = cacheNamespace;
        cacheTableName = typeof(TValue).Name;
        cacheTableRowKeyName = nameofKeyProperty;

        PrimeCache();
    }

    public Task<IEnumerable<TValue>> Set(IEnumerable<TValue> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        var keyValue = GetCollectionKeyValue(collection);
        return Set(keyValue, collection);
    }

    public async Task<IEnumerable<TValue>> Set(TKey keyValue, IEnumerable<TValue> collection)
    {
        ArgumentNullException.ThrowIfNull(keyValue);
        ArgumentNullException.ThrowIfNull(collection);

        cache[keyValue] = collection;

        await SetFile(keyValue, collection);

        return collection;
    }

    public Task<IEnumerable<TValue>> Append(IEnumerable<TValue> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var keyValue = GetCollectionKeyValue(collection);

        return Append(keyValue, collection);
    }

    public async Task<IEnumerable<TValue>> Append(TKey keyValue, IEnumerable<TValue> collection)
    {
        ArgumentNullException.ThrowIfNull(keyValue);
        ArgumentNullException.ThrowIfNull(collection);

        if (!cache.TryGetValue(keyValue, out var existingCollection))
        {
            throw new InvalidOperationException($"Key {keyValue} not found in cache.");
        }

        await AppendFile(keyValue, collection);

        var updatedCollection = cache[keyValue] = existingCollection.Concat(collection);

        return updatedCollection;
    }

    public bool Has(TKey key) => cache.ContainsKey(key);

    public Task<IEnumerable<TValue>> Get(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return cache.TryGetValue(key, out var value)
            ? Task.FromResult(value)
            : throw new KeyNotFoundException($"Key {key} not found in cache.");
    }

    private TKey GetCollectionKeyValue(IEnumerable<TValue> collection)
    {
        var keyProperty = typeof(TValue).GetProperty(cacheTableRowKeyName)
            ?? throw new KeyNotFoundException($"Property {cacheTableRowKeyName} not found on type {typeof(TValue).Name}.");

        var distinctKeyValues = collection
            .Select(item => (TKey?)keyProperty.GetValue(item))
            .Distinct()
            .ToList();

        return distinctKeyValues.Count switch
        {
            0 => throw new InvalidDataException("Collection is empty."),
            1 => distinctKeyValues[0] ?? throw new InvalidDataException("Key value is null."),
            _ => throw new InvalidDataException("Collection contains multiple distinct key values.")
        };
    }

    private void PrimeCache()
    {
        if (!cache.IsEmpty)
        {
            throw new InvalidOperationException("Cache is already populated.");
        }

        var namespaceDirectoryPath = Path.Combine(cacheRootPath, cacheTableName, cacheNamespace);

        if (!Directory.Exists(namespaceDirectoryPath))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(namespaceDirectoryPath, $"*.{CACHE_FILE_EXTENSION}"))
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var key = (TKey)Convert.ChangeType(fileName, typeof(TKey));

            var values = File.ReadLines(filePath)
                .Select(line => JsonSerializer.Deserialize<TValue>(line))
                .Where(value => value is not null)
                .ToList();

            if (values.Count > 0)
            {
                cache[key] = values!;
            }
        }
    }

    private async Task SetFile(TKey keyValue, IEnumerable<TValue> collection)
    {
        var filePath = GetCacheFilePath(keyValue);
        var directory = Path.GetDirectoryName(filePath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var cacheFileLines = collection.Select(item => JsonSerializer.Serialize(item));

        await File.WriteAllLinesAsync(filePath, cacheFileLines);
    }

    private async Task AppendFile(TKey keyValue, IEnumerable<TValue> collection)
    {
        var filePath = GetCacheFilePath(keyValue);
        var directory = Path.GetDirectoryName(filePath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var cacheFileLines = collection.Select(item => JsonSerializer.Serialize(item));

        await File.AppendAllLinesAsync(filePath, cacheFileLines);
    }

    private string GetCacheFilePath(TKey keyValue) =>
        Path.Combine(cacheRootPath, cacheTableName, cacheNamespace, $"{keyValue}.{CACHE_FILE_EXTENSION}");
}
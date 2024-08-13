using System.Collections.Concurrent;
using System.Text.Json;

namespace Data.TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private const string CACHE_FILE_EXTENSION = "txt";

    private readonly string cacheRootPath;
    private readonly string cacheNamespace;
    private readonly string cacheTableName = typeof(TValue).Name;
    private readonly string cacheInstanceKey;

    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<TKey, IEnumerable<TValue>>> memoryCache = [];

    public TableFileCache(string cacheRootPath, string cacheNamespace)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheRootPath);
        ArgumentException.ThrowIfNullOrEmpty(cacheNamespace);

        this.cacheRootPath = cacheRootPath;
        this.cacheNamespace = cacheNamespace;
        cacheInstanceKey = cacheRootPath + cacheNamespace;

        memoryCache[cacheInstanceKey] = [];
    }

    public bool Has(TKey key) => memoryCache[cacheInstanceKey].ContainsKey(key) || File.Exists(GetCacheFilePath(key));

    public Task<IEnumerable<TValue>> Get(TKey key)
        => memoryCache[cacheInstanceKey].TryGetValue(key, out var value)
            ? Task.FromResult(value)
            : GetFromFile(key);

    private async Task<IEnumerable<TValue>> GetFromFile(TKey key)
    {
        var filePath = GetCacheFilePath(key);
        var fileLines = await File.ReadAllLinesAsync(filePath);
        var values = fileLines
            .Select(line => JsonSerializer.Deserialize<TValue>(line))
            .Where(value => value != null)
            .ToList();

        return memoryCache[cacheInstanceKey][key] = values!;
    }

    public async Task<IEnumerable<TValue>> Set(TKey key, IEnumerable<TValue> value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        await WriteToFileAsync(key, value);

        return memoryCache[cacheInstanceKey][key] = value;
    }

    public async Task<IEnumerable<TValue>> Append(TKey key, IEnumerable<TValue> value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        await AppendToFileAsync(key, value);

        return memoryCache[cacheInstanceKey][key] = memoryCache[cacheInstanceKey][key].Concat(value);
    }

    private async Task WriteToFileAsync(TKey key, IEnumerable<TValue> value)
    {
        var fullFilePath = GetCacheFilePath(key);
        var filePathDirectory = Path.GetDirectoryName(fullFilePath)
            ?? throw new InvalidOperationException("No directory path found in full cache file path.");

        if (!Directory.Exists(filePathDirectory))
        {
            Directory.CreateDirectory(filePathDirectory);
        }

        var cacheFileLines = value.Select(item => JsonSerializer.Serialize(item));

        await File.WriteAllLinesAsync(fullFilePath, cacheFileLines);
    }

    private async Task AppendToFileAsync(TKey key, IEnumerable<TValue> value)
    {
        var fullFilePath = GetCacheFilePath(key);
        var filePathDirectory = Path.GetDirectoryName(fullFilePath)
            ?? throw new InvalidOperationException("No directory path found in full cache file path.");

        if (!Directory.Exists(filePathDirectory))
        {
            Directory.CreateDirectory(filePathDirectory);
        }

        var cacheFileLines = value.Select(item => JsonSerializer.Serialize(item));

        await File.AppendAllLinesAsync(fullFilePath, cacheFileLines);
    }

    private string GetCacheFilePath(TKey keyValue)
        => Path.Combine(cacheRootPath, cacheTableName, cacheNamespace, $"{keyValue}.{CACHE_FILE_EXTENSION}");
}
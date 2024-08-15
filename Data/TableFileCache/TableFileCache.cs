using System.Collections.Concurrent;
using System.Text.Json;
using Data.TableFileCache.GenericMemoryCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private const string CACHE_FILE_EXTENSION = "txt";

    private readonly string cacheRootPath;
    private readonly string cacheNamespace;
    private readonly string cacheTableName = typeof(TValue).Name;
    private readonly string cacheInstanceKey;

    // TODO setup DI config
    private static readonly ConcurrentDictionary<string, IGenericMemoryCache<TKey, IEnumerable<TValue>>> memoryCache = [];

    public TableFileCache( 
        string cacheRootPath, 
        string? cacheNamespace = null,
        IOptions<MemoryCacheEntryOptions>? memoryCacheEntryOptions = null,
        IOptions<MemoryCacheOptions>? memoryCacheOptions = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheRootPath);

        this.cacheRootPath = cacheRootPath;
        this.cacheNamespace = cacheNamespace ?? String.Empty;
        this.cacheInstanceKey = cacheRootPath + cacheNamespace;

        memoryCache[cacheInstanceKey] = new GenericMemoryCache<TKey, IEnumerable<TValue>>(
            memoryCacheOptions?.Value!, 
            memoryCacheEntryOptions?.Value!);
    }

    public bool Has(TKey key) => memoryCache[cacheInstanceKey].TryGet(key, out IEnumerable<TValue>? _) || 
        File.Exists(GetCacheFilePath(key));

    public Task<IEnumerable<TValue>> Get(TKey key)
        => memoryCache[cacheInstanceKey].TryGet(key, out var value)
            ? Task.FromResult(value ?? throw new InvalidOperationException($"{nameof(value)} should not be null."))
            : GetAndCacheFromFile(key);

    private async Task<IEnumerable<TValue>> GetAndCacheFromFile(TKey key)
    {
        var filePath = GetCacheFilePath(key);
        var fileLines = await File.ReadAllLinesAsync(filePath);
        var values = fileLines
            .Select(line => JsonSerializer.Deserialize<TValue>(line))
            .Select(value => value ?? throw new InvalidOperationException("Deserialzing record failed."))
            .ToList();

        return memoryCache[cacheInstanceKey][key] = values;
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

        return memoryCache[cacheInstanceKey][key] = (memoryCache[cacheInstanceKey][key] ?? 
            throw new KeyNotFoundException()).Concat(value);
    }

    public Task<IEnumerable<TValue>> Put(TKey key, IEnumerable<TValue> value, bool append)
        => !append
            ? Set(key, value)
            : Append(key, value);

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
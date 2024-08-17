using System.Collections.Concurrent;
using System.Text.Json;
using Data.TableFileCache.DaylongCache;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private const string CACHE_FILE_EXTENSION = "txt";

    private readonly string cacheRootPath;

    private readonly string cacheRelativePath;

    private readonly string cacheTableName = typeof(TValue).Name;

    private readonly string cacheInstanceKey;

    // TODO can we decouple TableFileCache from using a DaylongCache, i.e. use IGenericMemoryCache instead, and have the consumers of TableFileCache instances inject the IGenericMemoryCache?
    private static readonly ConcurrentDictionary<string, DaylongCache<TKey, IEnumerable<TValue>>> memoryCache = [];

    public TableFileCache(IOptions<TableFileCacheOptions> tableCacheOptions, string? cacheNamespace = null)
    {
        ArgumentNullException.ThrowIfNull(tableCacheOptions, nameof(tableCacheOptions));

        this.cacheRootPath = tableCacheOptions.Value.CacheRootPath;
        this.cacheRelativePath = cacheNamespace ?? tableCacheOptions.Value.CacheNamespace ?? String.Empty;
        this.cacheInstanceKey = cacheRootPath + cacheRelativePath;

        memoryCache[cacheInstanceKey] = new DaylongCache<TKey, IEnumerable<TValue>>(
            tableCacheOptions.Value.DaylongCacheOptions);
    }

    public bool Has(TKey key) => memoryCache[cacheInstanceKey].TryGet(key, out IEnumerable<TValue>? _);

    public Task<IEnumerable<TValue>> Get(TKey key)
        => Task.FromResult(memoryCache[cacheInstanceKey].Get(key) ?? throw new KeyNotFoundException());

    /*
     * TODO what to do about this? old logic related to priming the cache. Problem is we only want to prime
     * the cache with this data if we know it's current. But we know it's not current during first run unless
     * the files have been written as of last trading day close.
     *
     * E.g. have option to prime cache
     *
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
            .Select(value => value ?? throw new InvalidOperationException("Deserializing record failed."))
            .ToList();

        return memoryCache[cacheInstanceKey][key] = values;
    }
    */

    public Task<IEnumerable<TValue>> Put(TKey key, IEnumerable<TValue> value, bool append = false)
        => !append
            ? Set(key, value)
            : Append(key, value);

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

    private string GetCacheFilePath(TKey keyValue)
        => Path.Combine(cacheRootPath, cacheTableName, cacheRelativePath, $"{keyValue}.{CACHE_FILE_EXTENSION}");

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
}
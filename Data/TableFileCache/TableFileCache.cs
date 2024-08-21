﻿using System.Collections.Concurrent;
using System.Text.Json;
using Data.TableFileCache.GenericMemoryCache;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private static readonly object fileLock = new object();

    private const string CACHE_FILE_EXTENSION = "txt";

    private readonly string cacheRootPath;

    private readonly string cacheRelativePath;

    private readonly string cacheTableName = typeof(TValue).Name;

    private readonly string cacheInstanceKey;

    private readonly bool cacheMissReadsFileCache;

    private static readonly ConcurrentDictionary<string, GenericMemoryCache<TKey, IEnumerable<TValue>>> memoryCache = [];

    public TableFileCache(IOptions<TableFileCacheOptions> tableCacheOptions, string? cacheNamespace = null)
    {
        ArgumentNullException.ThrowIfNull(tableCacheOptions, nameof(tableCacheOptions));

        this.cacheRootPath = tableCacheOptions.Value.CacheRootPath;
        this.cacheRelativePath = cacheNamespace ?? tableCacheOptions.Value.CacheNamespace ?? String.Empty;
        this.cacheInstanceKey = cacheRootPath + cacheRelativePath;
        this.cacheMissReadsFileCache = tableCacheOptions.Value.CacheMissReadsFileCache;

        // Cache instances are static; do not blow away existing cache each new TableFileCache instantiation
        if (!memoryCache.ContainsKey(cacheInstanceKey))
        {
            memoryCache[cacheInstanceKey] = new GenericMemoryCache<TKey, IEnumerable<TValue>>();
        }
    }

    public async Task<IEnumerable<TValue>?> TryGetValue(TKey key)
    {
        if (memoryCache[cacheInstanceKey].TryGetValue(key, out IEnumerable<TValue>? value))
        {
            return value;
        }

        if (!cacheMissReadsFileCache)
        {
            return null;
        }

        lock (fileLock)
        {
            if (!File.Exists(GetCacheFilePath(key)))
            {
                return null;
            }
        }

        return await GetAndCacheFromFile(key);
    }

    public async Task<IEnumerable<TValue>> Get(TKey key) => await TryGetValue(key) ?? throw new KeyNotFoundException();

    private async Task<IEnumerable<TValue>> GetAndCacheFromFile(TKey key)
    {
        if (!cacheMissReadsFileCache)
        {
            throw new InvalidOperationException($"{nameof(TableFileCache)} configured file cache entries as write-only.");
        }

        var filePath = GetCacheFilePath(key);
        var fileLines = await File.ReadAllLinesAsync(filePath);
        var values = fileLines
            .Select(line => JsonSerializer.Deserialize<TValue>(line))
            .Select(value => value ?? throw new InvalidOperationException("Deserializing record failed."))
            .ToList();

        return memoryCache[cacheInstanceKey][key] = values;
    }

    public Task<IEnumerable<TValue>> Put(TKey key, IEnumerable<TValue> value, bool append = false)
        => !append
            ? Set(key, value)
            : Append(key, value);

    private async Task<IEnumerable<TValue>> Set(TKey key, IEnumerable<TValue> value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        await WriteToFileAsync(key, value);

        return memoryCache[cacheInstanceKey][key] = value;
    }

    private async Task<IEnumerable<TValue>> Append(TKey key, IEnumerable<TValue> value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        await AppendToFileAsync(key, value);

        return memoryCache[cacheInstanceKey][key] = (memoryCache[cacheInstanceKey][key] ??
            throw new KeyNotFoundException()).Concat(value);
    }

    private string GetCacheFilePath(TKey keyValue) => GetCacheFilePath($"{keyValue}");

    private string GetCacheFilePath(string key)
        => Path.Combine(cacheRootPath, cacheTableName, cacheRelativePath, $"{key}.{CACHE_FILE_EXTENSION}");

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
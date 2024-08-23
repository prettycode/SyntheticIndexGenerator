using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private const string fileExtensionSansDot = "txt";

    private readonly string rootPath;

    private readonly string relativePath;

    private readonly string tableName = typeof(TValue).Name;

    private readonly string instanceKey;

    private readonly bool missReadsFileCache;

    private static readonly ConcurrentDictionary<string, AbsoluteExpirationCache<TKey, IEnumerable<TValue>>> cacheInstances = [];

    private readonly DailyExpirationCacheOptions dailyExpirationCacheOptions;

    public TableFileCache(IOptions<TableFileCacheOptions> tableCacheOptions, string? cacheNamespace = null)
    {
        DateTimeOffset GetNextExpirationDateTimeOffset()
        {
            var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, dailyExpirationCacheOptions.TimeZone);

            var todayAtSpecifiedTime = new DateTimeOffset(
                now.Date.Add(dailyExpirationCacheOptions.TimeOfDay.ToTimeSpan()),
                dailyExpirationCacheOptions.TimeZone.GetUtcOffset(now.DateTime));

            if (now >= todayAtSpecifiedTime)
            {
                return todayAtSpecifiedTime.AddDays(1);
            }

            return todayAtSpecifiedTime;
        }

        ArgumentNullException.ThrowIfNull(tableCacheOptions, nameof(tableCacheOptions));

        rootPath = tableCacheOptions.Value.CacheRootPath;
        relativePath = cacheNamespace ?? tableCacheOptions.Value.CacheNamespace ?? string.Empty;
        instanceKey = rootPath + relativePath;
        missReadsFileCache = tableCacheOptions.Value.CacheMissReadsFileCache;
        dailyExpirationCacheOptions = tableCacheOptions.Value.DailyExpirationOptions;

        if (!cacheInstances.ContainsKey(instanceKey))
        {
            cacheInstances[instanceKey] = new AbsoluteExpirationCache<TKey, IEnumerable<TValue>>(
                GetNextExpirationDateTimeOffset,
                memoryCacheOptions: dailyExpirationCacheOptions.MemoryCacheOptions);
        }
    }

    public async Task<IEnumerable<TValue>> Get(TKey key) => await TryGetValue(key) ?? throw new KeyNotFoundException();

    public async Task<IEnumerable<TValue>?> TryGetValue(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (cacheInstances[instanceKey].TryGetValue(key, out IEnumerable<TValue>? value))
        {
            return value;
        }

        if (!missReadsFileCache)
        {
            return null;
        }

        if (!File.Exists(GetCacheFilePath(key)))
        {
            return null;
        }

        var filePath = GetCacheFilePath(key);
        var fileLines = await ThreadSafeFile.ReadAllLinesAsync(filePath);

        var values = fileLines.Select(line => JsonSerializer.Deserialize<TValue>(line)
            ?? throw new InvalidOperationException("Deserializing record failed."));

        return cacheInstances[instanceKey][key] = values;
    }

    public async Task<IEnumerable<TValue>> Put(TKey key, IEnumerable<TValue> value, bool append = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var fullFilePath = GetCacheFilePath(key);
        var filePathDirectory = Path.GetDirectoryName(fullFilePath)
            ?? throw new InvalidOperationException("No directory path found in full cache file path.");

        Directory.CreateDirectory(filePathDirectory);

        var cacheFileLines = value.Select(item => JsonSerializer.Serialize(item));

        if (append)
        {
            await ThreadSafeFile.AppendAllLinesAsync(fullFilePath, cacheFileLines);

            return cacheInstances[instanceKey][key]
                = (cacheInstances[instanceKey][key] ?? throw new KeyNotFoundException()).Concat(value);
        }

        await ThreadSafeFile.WriteAllLinesAsync(fullFilePath, cacheFileLines);

        return cacheInstances[instanceKey][key] = value;
    }

    private string GetCacheFilePath(TKey keyValue) => GetCacheFilePath($"{keyValue}");

    private string GetCacheFilePath(string key)
        => Path.Combine(rootPath, tableName, relativePath, $"{key}.{fileExtensionSansDot}");
}
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private const string CACHE_FILE_EXTENSION = "txt";

    private readonly string cacheRootPath;

    private readonly string cacheRelativePath;

    private readonly string cacheTableName = typeof(TValue).Name;

    private readonly string cacheInstanceKey;

    private readonly bool cacheMissReadsFileCache;

    private static readonly ConcurrentDictionary<string, DailyExpirationCache<TKey, IEnumerable<TValue>>> memoryCache = [];

    private readonly DailyExpirationCacheOptions dailyExpirationCacheOptions;

    public TableFileCache(IOptions<TableFileCacheOptions> tableCacheOptions, string? cacheNamespace = null)
    {
        DateTimeOffset GetNextExpirationDateTimeOffset()
        {
            var destinationTimeZone = dailyExpirationCacheOptions.TimeZone;
            var zonedDateTimeOffset = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, destinationTimeZone);
            var zonedDateTimeOffsetMidnight = zonedDateTimeOffset.Date;
            var zonedTodayAtTimeOfDay = zonedDateTimeOffsetMidnight.Add(dailyExpirationCacheOptions.TimeOfDay.ToTimeSpan());
            var nextExpiration = zonedTodayAtTimeOfDay > zonedDateTimeOffset
                ? zonedTodayAtTimeOfDay
                : zonedTodayAtTimeOfDay.AddDays(1);

            return new DateTimeOffset(nextExpiration, destinationTimeZone.GetUtcOffset(nextExpiration));
        }

        ArgumentNullException.ThrowIfNull(tableCacheOptions, nameof(tableCacheOptions));

        cacheRootPath = tableCacheOptions.Value.CacheRootPath;
        cacheRelativePath = cacheNamespace ?? tableCacheOptions.Value.CacheNamespace ?? string.Empty;
        cacheInstanceKey = cacheRootPath + cacheRelativePath;
        cacheMissReadsFileCache = tableCacheOptions.Value.CacheMissReadsFileCache;
        dailyExpirationCacheOptions = tableCacheOptions.Value.DailyExpirationOptions;

        // Cache instances are static; do not blow away existing cache each new TableFileCache instantiation
        if (!memoryCache.ContainsKey(cacheInstanceKey))
        {
            memoryCache[cacheInstanceKey] = new DailyExpirationCache<TKey, IEnumerable<TValue>>(
                GetNextExpirationDateTimeOffset,
                memoryCacheOptions: dailyExpirationCacheOptions.MemoryCacheOptions);
        }
    }

    public async Task<IEnumerable<TValue>> Get(TKey key) => await TryGetValue(key) ?? throw new KeyNotFoundException();

    public async Task<IEnumerable<TValue>?> TryGetValue(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (memoryCache[cacheInstanceKey].TryGetValue(key, out IEnumerable<TValue>? value))
        {
            return value;
        }

        if (!cacheMissReadsFileCache)
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

        return memoryCache[cacheInstanceKey][key] = values;
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

            return memoryCache[cacheInstanceKey][key]
                = (memoryCache[cacheInstanceKey][key] ?? throw new KeyNotFoundException()).Concat(value);
        }

        await ThreadSafeFile.WriteAllLinesAsync(fullFilePath, cacheFileLines);

        return memoryCache[cacheInstanceKey][key] = value;
    }

    private string GetCacheFilePath(TKey keyValue) => GetCacheFilePath($"{keyValue}");

    private string GetCacheFilePath(string key)
        => Path.Combine(cacheRootPath, cacheTableName, cacheRelativePath, $"{key}.{CACHE_FILE_EXTENSION}");
}
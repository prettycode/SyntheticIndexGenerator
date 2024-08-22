using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

public class TableFileCache<TKey, TValue> where TKey : notnull
{
    private const string CACHE_FILE_EXTENSION = "txt";

    private readonly string cacheRootPath;

    private readonly string cacheRelativePath;

    private readonly string cacheTableName = typeof(TValue).Name;

    private readonly string cacheInstanceKey;

    private readonly bool cacheMissReadsFileCache;

    private static readonly ConcurrentDictionary<string, GenericMemoryCache<TKey, IEnumerable<TValue>>> memoryCache = [];

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new();

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

        this.cacheRootPath = tableCacheOptions.Value.CacheRootPath;
        this.cacheRelativePath = cacheNamespace ?? tableCacheOptions.Value.CacheNamespace ?? String.Empty;
        this.cacheInstanceKey = cacheRootPath + cacheRelativePath;
        this.cacheMissReadsFileCache = tableCacheOptions.Value.CacheMissReadsFileCache;
        this.dailyExpirationCacheOptions = tableCacheOptions.Value.DailyExpirationOptions;

        // Cache instances are static; do not blow away existing cache each new TableFileCache instantiation
        if (!memoryCache.ContainsKey(cacheInstanceKey))
        {
            memoryCache[cacheInstanceKey] = new GenericMemoryCache<TKey, IEnumerable<TValue>>(
                GetNextExpirationDateTimeOffset,
                dailyExpirationCacheOptions.MemoryCacheOptions);
        }
    }

    public async Task<IEnumerable<TValue>?> TryGetValue(TKey key)
    {
        async Task<IEnumerable<TValue>> GetAndCacheFromFile(TKey key)
        {
            if (!cacheMissReadsFileCache)
            {
                throw new InvalidOperationException($"{nameof(TableFileCache)} configured file cache entries as write-only.");
            }

            var filePath = GetCacheFilePath(key);
            var fileLock = fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
            string[] fileLines;

            await fileLock.WaitAsync();

            try
            {
                fileLines = await File.ReadAllLinesAsync(filePath);
            }
            finally
            {
                fileLock.Release();
            }

            var values = fileLines
                .Select(line => JsonSerializer.Deserialize<TValue>(line)
                    ?? throw new InvalidOperationException("Deserializing record failed."));

            return memoryCache[cacheInstanceKey][key] = values;
        }

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

        return await GetAndCacheFromFile(key);
    }

    public async Task<IEnumerable<TValue>> Get(TKey key) => await TryGetValue(key) ?? throw new KeyNotFoundException();

    public async Task<IEnumerable<TValue>> Put(TKey key, IEnumerable<TValue> value, bool append = false)
    {
        async Task WriteToFileAsync(TKey key, IEnumerable<TValue> value, bool append)
        {
            var fullFilePath = GetCacheFilePath(key);
            var filePathDirectory = Path.GetDirectoryName(fullFilePath)
                ?? throw new InvalidOperationException("No directory path found in full cache file path.");

            Directory.CreateDirectory(filePathDirectory);

            var cacheFileLines = value.Select(item => JsonSerializer.Serialize(item));
            var fileLock = fileLocks.GetOrAdd(fullFilePath, _ => new SemaphoreSlim(1, 1));

            await fileLock.WaitAsync();

            try
            {
                if (append)
                {
                    await File.AppendAllLinesAsync(fullFilePath, cacheFileLines);
                }
                else
                {
                    await File.WriteAllLinesAsync(fullFilePath, cacheFileLines);
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        await WriteToFileAsync(key, value, append);

        if (append)
        {
            return memoryCache[cacheInstanceKey][key]
                = (memoryCache[cacheInstanceKey][key] ?? throw new KeyNotFoundException()).Concat(value);
        }

        return memoryCache[cacheInstanceKey][key] = value;
    }

    private string GetCacheFilePath(TKey keyValue) => GetCacheFilePath($"{keyValue}");

    private string GetCacheFilePath(string key)
        => Path.Combine(cacheRootPath, cacheTableName, cacheRelativePath, $"{key}.{CACHE_FILE_EXTENSION}");
}
using System.Collections.ObjectModel;
using System.Text.Json;

public class FundHistoryRepository
{
    private enum CacheType
    {
        Dividend,
        Price,
        Split
    }

    private readonly string cachePath;

    public FundHistoryRepository(string cachePath = @"../../../data/")
    {
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            throw new ArgumentNullException(nameof(cachePath));
        }

        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }

        this.cachePath = cachePath;
    }

    public IEnumerable<string> GetCacheKeys()
    {
        var cacheFilePath = this.GetCacheFilePath("*", CacheType.Price);
        var dirNameOnly = Path.GetDirectoryName(cacheFilePath);
        var fileNameOnly = Path.GetFileName(cacheFilePath);
        var matchingFiles = Directory.GetFiles(dirNameOnly!, fileNameOnly);

        return matchingFiles.Select(file => Path.GetFileNameWithoutExtension(file));
    }

    public async Task<FundHistory?> Get(string ticker)
    {
        FundHistory history = new(ticker);
        ReadOnlyDictionary<CacheType, string> cacheFilePaths = this.GetCacheFilePaths(ticker);

        if (!File.Exists(cacheFilePaths[CacheType.Price]))
        {
            return null;
        }

        var results = await Task.WhenAll([
            this.GetRawCacheContent(ticker, CacheType.Dividend),
            this.GetRawCacheContent(ticker, CacheType.Price),
            this.GetRawCacheContent(ticker, CacheType.Split)
        ]);

        history.Dividends = results[0].Select(line => JsonSerializer.Deserialize<DividendRecord>(line)).ToList();
        history.Prices = results[1].Select(line => JsonSerializer.Deserialize<PriceRecord>(line)).ToList();
        history.Splits = results[2].Select(line => JsonSerializer.Deserialize<SplitRecord>(line)).ToList();

        FundHistoryRepository.Inspect(history);

        return history;
    }

    public async Task Put(FundHistory fundHistory)
    {
        FundHistoryRepository.Inspect(fundHistory);

        ReadOnlyDictionary<CacheType, string> cacheFilePaths = this.GetCacheFilePaths(fundHistory.Ticker);

        var serializedDividends = fundHistory.Dividends.Select(div => JsonSerializer.Serialize<DividendRecord>(div));
        var serializedPrices = fundHistory.Prices.Select(price => JsonSerializer.Serialize<PriceRecord>(price));
        var serializedSplits = fundHistory.Splits.Select(split => JsonSerializer.Serialize<SplitRecord>(split));

        await Task.WhenAll(
        [
            File.AppendAllLinesAsync(cacheFilePaths[CacheType.Dividend], serializedDividends),
            File.AppendAllLinesAsync(cacheFilePaths[CacheType.Price], serializedPrices),
            File.AppendAllLinesAsync(cacheFilePaths[CacheType.Split], serializedSplits)
        ]);
    }

    private ReadOnlyDictionary<CacheType, string> GetCacheFilePaths(string ticker) => new(new Dictionary<CacheType, string>()
    {
        [CacheType.Dividend] = this.GetCacheFilePath(ticker, CacheType.Dividend),
        [CacheType.Price] = this.GetCacheFilePath(ticker, CacheType.Price),
        [CacheType.Split] = this.GetCacheFilePath(ticker, CacheType.Split),
    });

    private string GetCacheFilePath(string ticker, CacheType cacheType) => cacheType switch
    {
        CacheType.Dividend => Path.Combine(this.cachePath, $"dividend/{ticker}.txt"),
        CacheType.Price => Path.Combine(this.cachePath, $"price/{ticker}.txt"),
        CacheType.Split => Path.Combine(this.cachePath, $"split/{ticker}.txt"),
        _ => throw new NotImplementedException(),
    };

    private Task<string[]> GetRawCacheContent(string ticker, CacheType cacheType) => cacheType switch
    {
        CacheType.Dividend => File.ReadAllLinesAsync(this.GetCacheFilePath(ticker, CacheType.Dividend)),
        CacheType.Price => File.ReadAllLinesAsync(this.GetCacheFilePath(ticker, CacheType.Price)),
        CacheType.Split => File.ReadAllLinesAsync(this.GetCacheFilePath(ticker, CacheType.Split)),
        _ => throw new NotImplementedException(),
    };

    private static List<Exception> Inspect(FundHistory fundHistory)
    {
        // TODO ensure they're ordered!

        var exceptions = new List<Exception>();

        foreach (var div in fundHistory.Dividends)
        {
            if (div.Dividend == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(div)}.{nameof(div.Dividend)} on {div.DateTime}"));
            }

            if (div.DateTime == default)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(div)}.{nameof(div.DateTime)} on {div.DateTime}"));
            }
        }

        foreach (var price in fundHistory.Prices)
        {
            if (price.Open == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price)}.{nameof(price.Open)} on {price.DateTime}"));
            }

            if (price.Close == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price)}.{nameof(price.Close)} on {price.DateTime}"));
            }

            if (price.AdjustedClose == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price)}.{nameof(price.AdjustedClose)} on {price.DateTime}"));
            }

            if (price.High == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price)}.{nameof(price.High)} on {price.DateTime}"));
            }

            if (price.Low == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price)}.{nameof(price.Low)} on {price.DateTime}"));
            }

            if (price.DateTime == default)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(price)}.{nameof(price.DateTime)} on {price.DateTime}"));
            }
        }

        foreach (var split in fundHistory.Splits)
        {
            if (split.BeforeSplit == 0)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(split)}.{nameof(split.BeforeSplit)} on {split.DateTime}"));
            }

            if (split.AfterSplit == 0)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(split)}.{nameof(split.AfterSplit)} on {split.DateTime}"));
            }

            if (split.DateTime == default)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(split)}.{nameof(split.DateTime)} on {split.DateTime}"));
            }
        }

        foreach (var exception in exceptions)
        {
            Console.Beep();
            Console.WriteLine($"{fundHistory.Ticker}: {exception.Message}");
        }

        return exceptions;
    }

}
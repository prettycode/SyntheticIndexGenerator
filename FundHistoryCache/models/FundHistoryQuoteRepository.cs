using System.Collections.ObjectModel;
using System.Text.Json;

public class FundHistoryQuoteRepository
{
    private enum CacheType
    {
        Dividend,
        Price,
        Split
    }

    public string CachePath { get; private set; }

    public FundHistoryQuoteRepository(string cachePath)
    {
        ArgumentNullException.ThrowIfNull(cachePath);

        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }

        this.CachePath = cachePath;
    }

    public IEnumerable<string> GetCacheKeys()
    {
        var cacheFilePath = this.GetCacheFilePath("*", CacheType.Price);
        var dirNameOnly = Path.GetDirectoryName(cacheFilePath);
        var fileNameOnly = Path.GetFileName(cacheFilePath);
        var matchingFiles = Directory.GetFiles(dirNameOnly!, fileNameOnly);

        return matchingFiles.Select(file => Path.GetFileNameWithoutExtension(file));
    }

    public async Task<FundHistoryQuote?> Get(string ticker)
    {
        FundHistoryQuote history = new(ticker);
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

        history.Dividends = results[0].Select(line => JsonSerializer.Deserialize<FundHistoryQuoteDividendRecord>(line)).ToList();
        history.Prices = results[1].Select(line => JsonSerializer.Deserialize<FundHistoryQuotePriceRecord>(line)).ToList();
        history.Splits = results[2].Select(line => JsonSerializer.Deserialize<FundHistoryQuoteSplitRecord>(line)).ToList();

        FundHistoryQuoteRepository.Inspect(history);

        return history;
    }

    public async Task Put(FundHistoryQuote fundHistory)
    {
        FundHistoryQuoteRepository.Inspect(fundHistory);

        ReadOnlyDictionary<CacheType, string> cacheFilePaths = this.GetCacheFilePaths(fundHistory.Ticker);

        var serializedDividends = fundHistory.Dividends.Select(div => JsonSerializer.Serialize<FundHistoryQuoteDividendRecord>(div));
        var serializedPrices = fundHistory.Prices.Select(price => JsonSerializer.Serialize<FundHistoryQuotePriceRecord>(price));
        var serializedSplits = fundHistory.Splits.Select(split => JsonSerializer.Serialize<FundHistoryQuoteSplitRecord>(split));

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
        CacheType.Dividend => Path.Combine(this.CachePath, $"./dividend/{ticker}.txt"),
        CacheType.Price => Path.Combine(this.CachePath, $"./price/{ticker}.txt"),
        CacheType.Split => Path.Combine(this.CachePath, $"./split/{ticker}.txt"),
        _ => throw new NotImplementedException(),
    };

    private Task<string[]> GetRawCacheContent(string ticker, CacheType cacheType) => cacheType switch
    {
        CacheType.Dividend => File.ReadAllLinesAsync(this.GetCacheFilePath(ticker, CacheType.Dividend)),
        CacheType.Price => File.ReadAllLinesAsync(this.GetCacheFilePath(ticker, CacheType.Price)),
        CacheType.Split => File.ReadAllLinesAsync(this.GetCacheFilePath(ticker, CacheType.Split)),
        _ => throw new NotImplementedException(),
    };

    private static List<Exception> Inspect(FundHistoryQuote fundHistory)
    {
        var exceptions = new List<Exception>();
        var previousDateTime = DateTime.MinValue;

        foreach (var div in fundHistory.Dividends)
        {
            if (div.Dividend == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(div.Dividend)} record in {nameof(fundHistory.Dividends)} on {div.DateTime:yyyy-MM-dd}"));
            }

            if (div.DateTime == default)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(div.DateTime)} record in {nameof(fundHistory.Dividends)} on {div.DateTime:yyyy-MM-dd}"));
            }

            if (div.DateTime <= previousDateTime)
            {
                exceptions.Add(new ArgumentException($"Non-chronological {nameof(div.DateTime)} record in {nameof(fundHistory.Dividends)} on {div.DateTime:yyyy-MM-dd}"));
            }

            previousDateTime = div.DateTime;
        }

        previousDateTime = DateTime.MinValue;

        foreach (var price in fundHistory.Prices)
        {
            if (price.Open == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price.Open)} record in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
            }

            if (price.Close == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price.Close)} record in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
            }

            if (price.AdjustedClose == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price.AdjustedClose)} record in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
            }

            if (price.High == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price.High)} record in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
            }

            if (price.Low == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(price.Low)} record in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
            }

            if (price.DateTime == default)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(price.DateTime)} record in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
            }

            if (price.DateTime <= previousDateTime)
            {
                exceptions.Add(new ArgumentException($"Non-chronological {nameof(price.DateTime)} record in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
            }

            previousDateTime = price.DateTime;
        }

        previousDateTime = DateTime.MinValue;

        foreach (var split in fundHistory.Splits)
        {
            if (split.BeforeSplit == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(split.BeforeSplit)} record in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
            }

            if (split.AfterSplit == 0)
            {
                exceptions.Add(new ArgumentException($"Zero-dollar {nameof(split.AfterSplit)} record in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
            }

            if (split.DateTime == default)
            {
                exceptions.Add(new ArgumentException($"Invalid {nameof(split.DateTime)} record in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
            }

            if (split.DateTime <= previousDateTime)
            {
                exceptions.Add(new ArgumentException($"Non-chronological {nameof(split.DateTime)} record in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
            }

            previousDateTime = split.DateTime;
        }

        foreach (var exception in exceptions)
        {
            Console.Beep();
            Console.WriteLine($"{fundHistory.Ticker}: {exception.Message}");
        }

        return exceptions;
    }

}
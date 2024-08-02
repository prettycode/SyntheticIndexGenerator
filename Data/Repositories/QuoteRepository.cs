using System.Collections.ObjectModel;
using System.Text.Json;
using Data.Controllers;
using Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Data.Repositories
{
    public class QuoteRepository : IQuoteRepository
    {
        private enum CacheType
        {
            Dividend,
            Price,
            Split
        }

        private readonly ILogger<QuoteRepository> logger;

        private readonly string cachePath;

        public QuoteRepository(IOptions<QuoteRepositorySettings> settings, ILogger<QuoteRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(settings);

            cachePath = settings.Value.CacheDirPath;

            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            this.logger = logger;
        }

        public IEnumerable<string> GetAllTickers()
        {
            var cacheFilePath = GetCacheFilePath("*", CacheType.Price);
            var dirNameOnly = Path.GetDirectoryName(cacheFilePath);
            var fileNameOnly = Path.GetFileName(cacheFilePath);
            var matchingFiles = Directory.GetFiles(dirNameOnly!, fileNameOnly);

            return matchingFiles.Select(file => Path.GetFileNameWithoutExtension(file));
        }

        public bool Has(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            ReadOnlyDictionary<CacheType, string> cacheFilePaths = GetCacheFilePaths(ticker);

            return File.Exists(cacheFilePaths[CacheType.Price]);
        }

        public async Task<Quote> Get(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            if (!Has(ticker))
            {
                throw new KeyNotFoundException($"No record for {nameof(ticker)} \"{ticker}\".");
            }

            var rawCacheContent = await Task.WhenAll([
                GetRawCacheContent(ticker, CacheType.Dividend),
                GetRawCacheContent(ticker, CacheType.Price),
                GetRawCacheContent(ticker, CacheType.Split)
            ]);

            var history = new Quote(ticker)
            {
                Dividends = rawCacheContent[0].Select(line => JsonSerializer.Deserialize<QuoteDividend>(line)).ToList(),
                Prices = rawCacheContent[1].Select(line => JsonSerializer.Deserialize<QuotePrice>(line)).ToList(),
                Splits = rawCacheContent[2].Select(line => JsonSerializer.Deserialize<QuoteSplit>(line)).ToList()
            };

            Inspect(history);

            return history;
        }

        public async Task Append(Quote fundHistory) =>
            await Put(fundHistory, (path, lines) => File.AppendAllLinesAsync(path, lines));

        public async Task Replace(Quote fundHistory) =>
            await Put(fundHistory, (path, lines) => File.WriteAllLinesAsync(path, lines));

        private async Task Put(Quote fundHistory, Func<string, IEnumerable<string>, Task> fileOperation)
        {
            ArgumentNullException.ThrowIfNull(fundHistory);

            Inspect(fundHistory);

            ReadOnlyDictionary<CacheType, string> cacheFilePaths = GetCacheFilePaths(fundHistory.Ticker);

            foreach (var cacheType in Enum.GetValues<CacheType>())
            {
                var cacheFilePath = Path.GetDirectoryName(cacheFilePaths[cacheType]);

                if (!Directory.Exists(cacheFilePath))
                {
                    Directory.CreateDirectory(cacheFilePath!);
                }
            }

            var serializedDividends = fundHistory.Dividends.Select(div => JsonSerializer.Serialize(div));
            var serializedPrices = fundHistory.Prices.Select(price => JsonSerializer.Serialize(price));
            var serializedSplits = fundHistory.Splits.Select(split => JsonSerializer.Serialize(split));

            await Task.WhenAll([
                fileOperation(cacheFilePaths[CacheType.Dividend], serializedDividends),
                fileOperation(cacheFilePaths[CacheType.Price], serializedPrices),
                fileOperation(cacheFilePaths[CacheType.Split], serializedSplits)
            ]);
        }

        private ReadOnlyDictionary<CacheType, string> GetCacheFilePaths(string ticker) => new(new Dictionary<CacheType, string>()
        {
            [CacheType.Dividend] = GetCacheFilePath(ticker, CacheType.Dividend),
            [CacheType.Price] = GetCacheFilePath(ticker, CacheType.Price),
            [CacheType.Split] = GetCacheFilePath(ticker, CacheType.Split),
        });

        private string GetCacheFilePath(string ticker, CacheType cacheType) => cacheType switch
        {
            CacheType.Dividend => Path.Combine(cachePath, $"./dividend/{ticker}.txt"),
            CacheType.Price => Path.Combine(cachePath, $"./price/{ticker}.txt"),
            CacheType.Split => Path.Combine(cachePath, $"./split/{ticker}.txt"),
            _ => throw new NotImplementedException(),
        };

        private Task<string[]> GetRawCacheContent(string ticker, CacheType cacheType) => cacheType switch
        {
            CacheType.Dividend => File.ReadAllLinesAsync(GetCacheFilePath(ticker, CacheType.Dividend)),
            CacheType.Price => File.ReadAllLinesAsync(GetCacheFilePath(ticker, CacheType.Price)),
            CacheType.Split => File.ReadAllLinesAsync(GetCacheFilePath(ticker, CacheType.Split)),
            _ => throw new NotImplementedException(),
        };

        private List<Exception> Inspect(Quote fundHistory)
        {
            var exceptions = new List<Exception>();
            var previousDateTime = DateTime.MinValue;

            foreach (var div in fundHistory.Dividends)
            {
                if (div.Dividend == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(div.Dividend)} in {nameof(fundHistory.Dividends)} on {div.DateTime:yyyy-MM-dd}"));
                }

                if (div.DateTime == default)
                {
                    exceptions.Add(new($"Invalid {nameof(div.DateTime)} in {nameof(fundHistory.Dividends)} on {div.DateTime:yyyy-MM-dd}"));
                }

                if (div.DateTime <= previousDateTime)
                {
                    exceptions.Add(new($"Non-chronological {nameof(div.DateTime)} in {nameof(fundHistory.Dividends)} on {div.DateTime:yyyy-MM-dd}"));
                }

                previousDateTime = div.DateTime;
            }

            previousDateTime = DateTime.MinValue;

            foreach (var price in fundHistory.Prices)
            {
                if (price.Open == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(price.Open)} in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
                }

                if (price.Close == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(price.Close)} in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
                }

                if (price.AdjustedClose == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(price.AdjustedClose)} in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
                }

                if (price.High == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(price.High)} in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
                }

                if (price.Low == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(price.Low)} in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
                }

                if (price.DateTime == default)
                {
                    exceptions.Add(new($"Invalid {nameof(price.DateTime)} in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
                }

                if (price.DateTime <= previousDateTime)
                {
                    exceptions.Add(new($"Non-chronological {nameof(price.DateTime)} in {nameof(fundHistory.Prices)} on {price.DateTime:yyyy-MM-dd}"));
                }

                // Don't check volume; it's 0 for mutual funds

                previousDateTime = price.DateTime;
            }

            previousDateTime = DateTime.MinValue;

            foreach (var split in fundHistory.Splits)
            {
                if (split.BeforeSplit == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(split.BeforeSplit)} in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
                }

                if (split.AfterSplit == 0)
                {
                    exceptions.Add(new($"Zero-dollar {nameof(split.AfterSplit)} in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
                }

                if (split.DateTime == default)
                {
                    exceptions.Add(new($"Invalid {nameof(split.DateTime)} in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
                }

                if (split.DateTime <= previousDateTime)
                {
                    exceptions.Add(new($"Non-chronological {nameof(split.DateTime)} in {nameof(fundHistory.Splits)} on {split.DateTime:yyyy-MM-dd}"));
                }

                previousDateTime = split.DateTime;
            }

            foreach (var ex in exceptions)
            {
                logger.LogWarning(ex, "{ticker}", fundHistory.Ticker);
            }

            return exceptions;
        }

    }
}
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Data.Quotes
{
    internal class QuoteRepository : IQuoteRepository
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

        private async Task<Quote> Get(string ticker)
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

        public async Task<Quote> Get(string ticker, bool skipPastZeroVolume = false)
        {
            var unfilteredQuote = await Get(ticker);

            if (!skipPastZeroVolume)
            {
                return unfilteredQuote;
            }

            var lastZeroVolumeIndex = unfilteredQuote.Prices.FindLastIndex(item => item.Volume == 0);

            if (lastZeroVolumeIndex == -1)
            {
                return unfilteredQuote;
            }

            var lastZeroVolumeDate = unfilteredQuote.Prices[lastZeroVolumeIndex].DateTime;
            var filteredQuote = new Quote(ticker)
            {
                Dividends = unfilteredQuote.Dividends.Where(div => div.DateTime > lastZeroVolumeDate).ToList(),
                Prices = unfilteredQuote.Prices.Skip(lastZeroVolumeIndex + 1).ToList(),
                Splits = unfilteredQuote.Splits.Where(split => split.DateTime > lastZeroVolumeDate).ToList()
            };

            return filteredQuote;
        }

        public Task<Quote> Append(Quote fundHistory) =>
            Put(fundHistory, (path, lines) => File.AppendAllLinesAsync(path, lines))
                .ContinueWith(_ => Get(fundHistory.Ticker))
                .Unwrap();

        public Task<Quote> Replace(Quote fundHistory) =>
            Put(fundHistory, (path, lines) => File.WriteAllLinesAsync(path, lines)).ContinueWith(_ => fundHistory);

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

            logger.LogInformation("{ticker}: Appending or replace quotes.", fundHistory.Ticker);

            await Task.WhenAll([
                !serializedDividends.Any()
                    ? Task.FromResult(0)
                    : fileOperation(cacheFilePaths[CacheType.Dividend], serializedDividends),
                !serializedPrices.Any()
                    ? Task.FromResult(0)
                    : fileOperation(cacheFilePaths[CacheType.Price], serializedPrices),
                !serializedSplits.Any()
                    ? Task.FromResult(0)
                    : fileOperation(cacheFilePaths[CacheType.Split], serializedSplits)
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

        private Task<string[]> GetRawCacheContent(string ticker, CacheType cacheType)
        {
            var filePath = GetCacheFilePath(ticker, cacheType);

            return File.Exists(filePath)
                ? File.ReadAllLinesAsync(filePath)
                : Task.FromResult(Array.Empty<string>());
        }

        private void Inspect(Quote fundHistory)
        {
            var previousDateTime = DateTime.MinValue;

            foreach (var div in fundHistory.Dividends)
            {
                if (div.Dividend == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(div.Dividend),
                        nameof(fundHistory.Dividends),
                        div.DateTime,
                        fundHistory.Ticker);
                }

                if (div.DateTime == default)
                {
                    logger.LogWarning("Invalid {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(div.DateTime),
                        nameof(fundHistory.Dividends),
                        div.DateTime,
                        fundHistory.Ticker);
                }

                if (div.DateTime <= previousDateTime)
                {
                    logger.LogWarning("Non-chronological {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(div.DateTime),
                        nameof(fundHistory.Dividends),
                        div.DateTime,
                        fundHistory.Ticker);
                }

                previousDateTime = div.DateTime;
            }

            previousDateTime = DateTime.MinValue;

            foreach (var price in fundHistory.Prices)
            {
                if (price.Open == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(price.Open),
                        nameof(fundHistory.Prices),
                        price.DateTime,
                        fundHistory.Ticker);
                }

                if (price.Close == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(price.Close),
                        nameof(fundHistory.Prices),
                        price.DateTime,
                        fundHistory.Ticker);
                }

                if (price.AdjustedClose == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(price.AdjustedClose),
                        nameof(fundHistory.Prices),
                        price.DateTime,
                        fundHistory.Ticker);
                }

                if (price.High == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(price.High),
                        nameof(fundHistory.Prices),
                        price.DateTime,
                        fundHistory.Ticker);
                }

                if (price.Low == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(price.Low),
                        nameof(fundHistory.Prices),
                        price.DateTime,
                        fundHistory.Ticker);
                }

                if (price.DateTime == default)
                {
                    logger.LogWarning("Invalid {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(price.DateTime),
                        nameof(fundHistory.Prices),
                        price.DateTime,
                        fundHistory.Ticker);
                }

                if (price.DateTime <= previousDateTime)
                {
                    logger.LogWarning("Non-chronological {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(price.DateTime),
                        nameof(fundHistory.Prices),
                        price.DateTime,
                        fundHistory.Ticker);
                }

                previousDateTime = price.DateTime;
            }

            // Check volume of non-mutual funds
            if (fundHistory.Prices.Any(tick => tick.Volume != 0))
            {
                foreach (var price in fundHistory.Prices)
                {
                    if (price.Volume == 0)
                    {
                        logger.LogWarning("Zero {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                            nameof(price.Volume),
                            nameof(fundHistory.Prices),
                            price.DateTime,
                            fundHistory.Ticker);
                    }
                }
            }

            previousDateTime = DateTime.MinValue;

            foreach (var split in fundHistory.Splits)
            {
                if (split.BeforeSplit == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(split.BeforeSplit),
                        nameof(fundHistory.Splits),
                        split.DateTime,
                        fundHistory.Ticker);
                }

                if (split.AfterSplit == 0)
                {
                    logger.LogWarning("Zero-dollar {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(split.AfterSplit),
                        nameof(fundHistory.Splits),
                        split.DateTime,
                        fundHistory.Ticker);
                }

                if (split.DateTime == default)
                {
                    logger.LogWarning("Invalid {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(split.DateTime),
                        nameof(fundHistory.Splits),
                        split.DateTime,
                        fundHistory.Ticker);
                }

                if (split.DateTime <= previousDateTime)
                {
                    logger.LogWarning("Non-chronological {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                        nameof(split.DateTime),
                        nameof(fundHistory.Splits),
                        split.DateTime,
                        fundHistory.Ticker);
                }

                previousDateTime = split.DateTime;
            }
        }

    }
}
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableFileCache;

namespace Data.Quotes;

internal class QuoteRepository : IQuoteRepository
{
    private readonly TableFileCache<string, QuotePrice> pricesCache;

    private readonly ILogger<QuoteRepository> logger;

    public QuoteRepository(IOptions<QuoteRepositoryOptions> quoteRepositoryOptions, ILogger<QuoteRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(quoteRepositoryOptions);

        var tableCacheOptions = quoteRepositoryOptions.Value.TableCacheOptions;

        pricesCache = new(tableCacheOptions);

        this.logger = logger;
    }

    public async Task<Quote?> TryGetMemoryCacheQuote(string ticker)
    {
        var prices = await pricesCache.TryGetValue(ticker);

        if (prices == null)
        {
            logger.LogInformation("{ticker}: No quote in memory cache.", ticker);

            return null;
        }

        var history = new Quote(ticker)
        {
            Prices = prices.ToList()
        };

        Inspect(history);

        logger.LogInformation("{ticker}: {recordCount} record(s) in memory cache, {firstPeriod} to {lastPeriod}.",
            ticker,
            history.Prices.Count,
            $"{history.Prices[0].DateTime:yyyy-MM-dd}",
            $"{history.Prices[^1].DateTime:yyyy-MM-dd}");

        return history;
    }

    public async Task<Quote?> TryGetFileCacheQuote(string ticker)
    {
        var prices = await pricesCache.TryGetValue(ticker, true);

        if (prices == null)
        {
            logger.LogInformation("{ticker}: No quote in file cache.", ticker);

            return null;
        }

        var history = new Quote(ticker)
        {
            Prices = prices.ToList()
        };

        Inspect(history);

        logger.LogInformation("{ticker}: {recordCount} record(s) in file cache, {firstPeriod} to {lastPeriod}.",
            ticker,
            history.Prices.Count,
            $"{history.Prices[0].DateTime:yyyy-MM-dd}",
            $"{history.Prices[^1].DateTime:yyyy-MM-dd}");

        return history;
    }

    private async Task<Quote> Get(string ticker)
    {
        var history = new Quote(ticker)
        {
            // Intentionally cause exception if ticker not found
            Prices = (await pricesCache.Get(ticker)).ToList()
        };

        Inspect(history);

        return history;
    }

    public async Task<Quote> GetQuote(string ticker, bool skipPastZeroVolume = false)
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

    public async Task<Quote> PutQuote(Quote fundHistory, bool append = false)
    {
        var ticker = fundHistory.Ticker;
        var operation = append ? "Appending" : "Replacing";

        logger.LogInformation(
            "{ticker}: {operation} {recordCount} records in quote cache, {firstPeriod} to {lastPeriod}.",
            ticker,
            operation,
            fundHistory.Prices.Count,
            $"{fundHistory.Prices[0].DateTime:yyyy-MM-dd}",
            $"{fundHistory.Prices[^1].DateTime:yyyy-MM-dd}");

        if (fundHistory.Prices.Count > 0)
        {
            await pricesCache.Put(ticker, fundHistory.Prices, append);
        }

        return !append
            ? fundHistory
            : await Get(ticker);
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
                logger.LogError("Non-chronological {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
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
                logger.LogError("Non-chronological {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
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
                logger.LogError("Non-chronological {Property} in {Collection} on {Date:yyyy-MM-dd} for {Ticker}",
                    nameof(split.DateTime),
                    nameof(fundHistory.Splits),
                    split.DateTime,
                    fundHistory.Ticker);
            }

            previousDateTime = split.DateTime;
        }
    }

}
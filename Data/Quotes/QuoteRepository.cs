using Data.TableFileCache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Data.Quotes;

internal class QuoteRepository : IQuoteRepository
{
    private readonly TableFileCache<string, QuoteDividend> dividendsCache;
    private readonly TableFileCache<string, QuotePrice> pricesCache;
    private readonly TableFileCache<string, QuoteSplit> splitsCache;

    private readonly ILogger<QuoteRepository> logger;

    public QuoteRepository(IOptions<QuoteRepositoryOptions> quoteRepositoryOptions, ILogger<QuoteRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(quoteRepositoryOptions);

        dividendsCache = new(quoteRepositoryOptions.Value.TableCacheOptions);
        pricesCache = new(quoteRepositoryOptions.Value.TableCacheOptions);
        splitsCache = new(quoteRepositoryOptions.Value.TableCacheOptions);

        this.logger = logger;
    }

    public bool Has(string ticker) => pricesCache.Has(ticker);

    private async Task<Quote> Get(string ticker)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        if (!Has(ticker))
        {
            throw new KeyNotFoundException($"No record for {nameof(ticker)} \"{ticker}\".");
        }

        var history = new Quote(ticker)
        {
            Dividends = dividendsCache.Has(ticker)
                ? (await dividendsCache.Get(ticker)).ToList()
                : Enumerable.Empty<QuoteDividend>().ToList(),

            Prices = pricesCache.Has(ticker)
                ? (await pricesCache.Get(ticker)).ToList()
                : Enumerable.Empty<QuotePrice>().ToList(),

            Splits = splitsCache.Has(ticker)
                ? (await splitsCache.Get(ticker)).ToList()
                : Enumerable.Empty<QuoteSplit>().ToList(),
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

    public Task<Quote> Append(Quote fundHistory) => Put(fundHistory, true);

    public Task<Quote> Replace(Quote fundHistory) => Put(fundHistory, false);

    private async Task<Quote> Put(Quote fundHistory, bool append)
    {
        var ticker = fundHistory.Ticker;
        var operation = append ? "Appending" : "Replacing";

        logger.LogInformation("{ticker}: {operation} quotes.", ticker, operation);

        await Task.WhenAll(
            fundHistory.Dividends.Count == 0
                ? Task.FromResult(0)
                : dividendsCache.Put(ticker, fundHistory.Dividends, append),

            fundHistory.Prices.Count == 0
                ? Task.FromResult(0)
                : pricesCache.Put(ticker, fundHistory.Prices, append),

            fundHistory.Splits.Count == 0
                ? Task.FromResult(0)
                : splitsCache.Put(ticker, fundHistory.Splits, append));

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
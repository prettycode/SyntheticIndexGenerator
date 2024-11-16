using Data.Quotes;
using Data.SyntheticIndices;
using Microsoft.Extensions.Logging;

namespace Data.Returns;

internal class ReturnsService(
    IQuotesService quotesService,
    ISyntheticIndicesService syntheticIndexService,
    IReturnsCache returnRepository,
    ILogger<ReturnsService> logger)
        : IReturnsService
{
    public async Task<Dictionary<string, List<PeriodReturn>>> GetReturnsHistory(
        HashSet<string> tickers,
        PeriodType periodType,
        DateTime firstPeriod,
        DateTime lastPeriod)
    {
        ArgumentNullException.ThrowIfNull(tickers);

        var getReturnsHistoryTasks = tickers.ToDictionary(
            ticker => ticker,
            ticker => GetReturnsHistory(ticker, periodType, firstPeriod, lastPeriod)
        );

        var results = await Task.WhenAll(getReturnsHistoryTasks.Values);

        return getReturnsHistoryTasks
            .Keys
            .Zip(results, (ticker, returns) => new { ticker, returns })
            .ToDictionary(pair => pair.ticker, pair => pair.returns);
    }

    public async Task<List<PeriodReturn>> GetReturnsHistory(
        string ticker,
        PeriodType periodType,
        DateTime firstPeriod,
        DateTime lastPeriod)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        bool IsSyntheticReturnTicker(string ticker) => !IsSyntheticIndexTicker(ticker) &&
            (ticker.StartsWith('$') || ticker.StartsWith('#')) && !IsScriptedTicker(ticker);
        bool IsSyntheticIndexTicker(string ticker) => ticker.StartsWith("$^") && !IsScriptedTicker(ticker);
        bool IsScriptedTicker(string ticker) => ticker.Contains(',');
        bool IsQuoteTicker(string ticker) => !IsSyntheticIndexTicker(ticker) && !IsSyntheticReturnTicker(ticker) && !IsScriptedTicker(ticker);

        var cachedReturns = await returnRepository.TryGetValue(ticker, periodType);

        if (cachedReturns != null)
        {
            return await returnRepository.Get(ticker, periodType, firstPeriod, lastPeriod);
        }

        if (IsSyntheticReturnTicker(ticker))
        {
            // Synthetic returns must be pre-generated and put in the repo by returnRepository itself during its startup

            throw new KeyNotFoundException(
                $"No synthetic returns for ticker '{ticker}' and period '{periodType}' in repository.");
        }

        if (IsSyntheticIndexTicker(ticker))
        {
            var neededQuoteTickers = syntheticIndexService.GetSyntheticIndexBackfillTickers(ticker);

            await Task.WhenAll(neededQuoteTickers.Select(quoteTicker
                => GetReturnsHistory(quoteTicker, periodType, firstPeriod, lastPeriod)));

            var backfillTickers = syntheticIndexService.GetSyntheticIndexBackfillTickers(ticker, false);

            await CalculateAndPutReturnsForSyntheticIndexByPeriod(ticker, backfillTickers, periodType);

            return await GetReturnsHistory(ticker, periodType, firstPeriod, lastPeriod);
        }

        if (IsScriptedTicker(ticker))
        {
            var tickers = ticker.Split(',').ToHashSet();

            await Task.WhenAll(tickers.Select(quoteTicker
                => GetReturnsHistory(quoteTicker, periodType, firstPeriod, lastPeriod)));

            return await CalculateReturnsForSyntheticIndexByPeriod(tickers, periodType);
        }

        if (IsQuoteTicker(ticker))
        {
            var dailyPricesByTicker = await quotesService.GetDailyQuoteHistory(ticker);
            var returns = await CalculateAndPutReturnsForPeriodType(ticker, dailyPricesByTicker, periodType);

            return [.. returns];
        }

        throw new InvalidOperationException("All scenarios should have been handled.");
    }
    private async Task<List<PeriodReturn>> CalculateAndPutReturnsForSyntheticIndexByPeriod(
        string syntheticIndexTicker,
        HashSet<string> backfillTickers,
        PeriodType periodType)
    {
        var backfillReturnsTasks = backfillTickers
            .Select(ticker => returnRepository.TryGetValue(ticker, periodType));

        var backfillReturns = (await Task.WhenAll(backfillReturnsTasks))
            .Where(returns => returns != null)
            .ToList();

        var collatedReturns = backfillReturns
            .Select((returns, index) =>
                (returns, nextStartDate: index < backfillReturns.Count - 1
                    ? backfillReturns[index + 1]?.First().PeriodStart
                    : DateTime.MaxValue
                )
            )
            .SelectMany(item => item.returns!.TakeWhile(pair => pair.PeriodStart < item.nextStartDate));

        return [.. await returnRepository.Put(syntheticIndexTicker, collatedReturns.ToList(), periodType)];
    }

    private async Task<List<PeriodReturn>> CalculateReturnsForSyntheticIndexByPeriod(
        HashSet<string> backfillTickers,
        PeriodType periodType)
    {
        var backfillReturnsTasks = backfillTickers
            .Select(ticker => returnRepository.TryGetValue(ticker, periodType));

        var backfillReturns = (await Task.WhenAll(backfillReturnsTasks))
            .Where(returns => returns != null)
            .ToList();

        var collatedReturns = backfillReturns
            .Select((returns, index) =>
                (returns, nextStartDate: index < backfillReturns.Count - 1
                    ? backfillReturns[index + 1]?.First().PeriodStart
                    : DateTime.MaxValue
                )
            )
            .SelectMany(item => item.returns!.TakeWhile(pair => pair.PeriodStart < item.nextStartDate));

        return collatedReturns.ToList();
    }

    private async Task<PeriodReturn[]> CalculateAndPutReturnsForPeriodType(
        string ticker,
        IEnumerable<QuotePrice> dailyPrices,
        PeriodType periodType)
    {
        var returnsForPeriodType = ReturnsCalculations.CalculateReturnsForPeriodType(ticker, dailyPrices.Select(quote => (quote.DateTime, quote.AdjustedClose)), periodType);

        if (!returnsForPeriodType.Any())
        {
            logger.LogWarning("{ticker} has no computable return history for {periodType}", ticker, periodType);
        }
        else
        {
            await returnRepository.Put(ticker, returnsForPeriodType, periodType);
        }

        return [.. returnsForPeriodType];
    }
}
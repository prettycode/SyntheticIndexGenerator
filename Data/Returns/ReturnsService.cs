﻿using Data.Quotes;
using Data.SyntheticIndex;
using Microsoft.Extensions.Logging;

namespace Data.Returns;

internal class ReturnsService(
        IQuotesService quotesService,
        ISyntheticIndexService syntheticIndexService,
        IReturnRepository returnRepository,
        ILogger<ReturnsService> logger)
            : IReturnsService
{
    public async Task<Dictionary<string, List<PeriodReturn>>> GetReturnsHistory(
        HashSet<string> tickers,
        PeriodType periodType,
        DateTime startDate,
        DateTime endDate)
    {
        ArgumentNullException.ThrowIfNull(tickers);

        // TODO this is not parallelized; don't have awaits in here
        return await tickers
            .ToAsyncEnumerable()
            .SelectAwait(async ticker => new { ticker, returns = await GetReturnsHistory(ticker, periodType, startDate, endDate) })
            .ToDictionaryAsync(pair => pair.ticker, pair => pair.returns);
    }

    public async Task<List<PeriodReturn>> GetReturnsHistory(
        string ticker,
        PeriodType periodType,
        DateTime startDate,
        DateTime endDate)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        bool IsSyntheticReturnTicker(string ticker) => !IsSyntheticIndexTicker(ticker) && ticker.StartsWith('$');
        bool IsSyntheticIndexTicker(string ticker) => ticker.StartsWith("$^");
        bool IsQuoteTicker(string ticker) => !IsSyntheticIndexTicker(ticker) && !IsSyntheticReturnTicker(ticker);

        // Get previously-computed and -saved results, and return them

        if (returnRepository.Has(ticker, periodType))
        {
            return await returnRepository.Get(ticker, periodType, startDate, endDate);
        }

        // Compute, save results, and return them

        if (IsSyntheticReturnTicker(ticker))
        {
            // Daily synthetic returns do not exist; only monthly and yearly exist
            // TODO this will be a huge problem when generating synthetic returns for things like RSSB

            if (periodType == PeriodType.Daily)
            {
                return [];
            }

            // Synthetic returns are pre-generated put in the repo by returnRepository during its startup

            throw new InvalidOperationException(
                "Synthetic returns should already have been calculated and put in returns repository.");
        }

        if (IsSyntheticIndexTicker(ticker))
        {
            var backfillTickers = syntheticIndexService.GetIndexBackfillTickers(ticker).ToList();
            var backfillReturnTasks = backfillTickers.Select(ticker => GetReturnsHistory(ticker, periodType, startDate, endDate));
            var backfillReturns = await Task.WhenAll(backfillReturnTasks);

            return await CollateBackfillReturns(backfillTickers, periodType);
        }

        if (IsQuoteTicker(ticker))
        {
            var dailyPricesByTicker = await quotesService.GetDailyQuoteHistory(ticker);
            var returns = await CalculateAndPutReturnsForPeriodType(ticker, dailyPricesByTicker, periodType);

            return [.. returns];

        }

        throw new InvalidOperationException("All scenarios should have been handled.");
    }

    public HashSet<string> GetSyntheticIndexTickers() => syntheticIndexService.GetIndexTickers();

    private async Task<List<PeriodReturn>> CollateBackfillReturns(List<string> backfillTickers, PeriodType period)
    {
        var availableBackfillTickers = backfillTickers.Where(ticker => returnRepository.Has(ticker, period));
        var backfillReturns = await Task.WhenAll(availableBackfillTickers.Select(ticker => returnRepository.Get(ticker, period)));
        var collatedReturns = backfillReturns
            .Select((returns, index) =>
                (returns, nextStartDate: index < backfillReturns.Length - 1
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
        static List<PeriodReturn> GetPeriodOnlyReturns(List<PeriodReturn> returns, Func<DateTime, DateTime> adjustDate)
            => returns.Select(r => r with { PeriodStart = adjustDate(r.PeriodStart) }).ToList();

        static List<QuotePrice> GroupPricesByPeriod<TKey>(IEnumerable<QuotePrice> prices, Func<DateTime, TKey> keySelector)
            => prices
                .GroupBy(r => keySelector(r.DateTime))
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

        static List<PeriodReturn> CalculateReturns(string ticker, IEnumerable<QuotePrice> prices, PeriodType periodType)
        {
            static decimal CalculateChange(decimal x, decimal y) => (y - x) / x * 100m;

            return prices
                .Zip(prices.Skip(1), (prev, current) => new PeriodReturn
                {
                    Ticker = ticker,
                    PeriodStart = current.DateTime,
                    ReturnPercentage = CalculateChange(prev.AdjustedClose, current.AdjustedClose),
                    PeriodType = periodType
                })
                .ToList();
        }

        var groupedPrices = periodType switch
        {
            PeriodType.Daily => dailyPrices,
            PeriodType.Monthly => GroupPricesByPeriod(dailyPrices, d => new { d.Year, d.Month }),
            PeriodType.Yearly => GroupPricesByPeriod(dailyPrices, d => d.Year),
            _ => throw new NotImplementedException()
        };

        var periodReturns = CalculateReturns(ticker, groupedPrices, periodType);

        var returnsForPeriodType = periodType switch
        {
            PeriodType.Daily => periodReturns,
            PeriodType.Monthly => GetPeriodOnlyReturns(periodReturns, d => new DateTime(d.Year, d.Month, 1)),
            PeriodType.Yearly => GetPeriodOnlyReturns(periodReturns, d => new DateTime(d.Year, 1, 1)),
            _ => throw new NotImplementedException()
        };

        if (returnsForPeriodType.Count == 0)
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
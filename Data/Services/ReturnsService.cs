using Data.Models;
using Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Data.Services
{
    internal class ReturnsService(IReturnRepository returnRepository, ILogger<ReturnsService> logger) : IReturnsService
    {
        public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetReturns(
            Dictionary<string, IEnumerable<QuotePrice>> dailyPricesByTicker)
        {
            ArgumentNullException.ThrowIfNull(dailyPricesByTicker);

            return await dailyPricesByTicker
                .ToAsyncEnumerable()
                .ToDictionaryAwaitAsync(
                    keySelector: pair => ValueTask.FromResult(pair.Key),
                    elementSelector: async pair => await GetReturns(pair.Key, pair.Value)
                );
        }

        public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetReturns(HashSet<string> tickers)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            var result = new Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>();

            foreach (var ticker in tickers)
            {
                var periodReturns = new Dictionary<PeriodType, PeriodReturn[]?>();

                foreach (PeriodType periodType in Enum.GetValues<PeriodType>())
                {
                    periodReturns[periodType] = await GetReturns(ticker, periodType);
                }

                result[ticker] = periodReturns;
            }

            return result;
        }

        public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetSyntheticIndexReturns(
            HashSet<string> syntheticTickers,
            Dictionary<string, Dictionary<string, IEnumerable<QuotePrice>>> syntheticConstituentDailyPricesByTicker)
        {
            async Task<Dictionary<PeriodType, PeriodReturn[]?>> GetReturnsByPeriodType(string backFillTicker)
            {
                var returns = new Dictionary<PeriodType, PeriodReturn[]?>();

                foreach (var periodType in Enum.GetValues<PeriodType>())
                {
                    returns[periodType] = returnRepository.Has(backFillTicker, periodType)
                        ? [.. await returnRepository.Get(backFillTicker, periodType)]
                        : null;
                }

                return returns;
            }

            ArgumentNullException.ThrowIfNull(syntheticTickers);
            ArgumentNullException.ThrowIfNull(syntheticConstituentDailyPricesByTicker);

            static Dictionary<string, HashSet<string>> GetConstituentTickersBySyntheticTicker(HashSet<string> neededSyntheticIndexTickers)
                => SyntheticIndex.GetSyntheticIndices().Where(index => neededSyntheticIndexTickers.Contains(index.Ticker))
                    .ToDictionary(index => index.Ticker, index => index.BackFillTickers.ToHashSet());

            var constituentTickersBySyntheticTicker = GetConstituentTickersBySyntheticTicker(syntheticTickers);

            var dedupedSyntheticConstituentDailyPricesByTicker = syntheticConstituentDailyPricesByTicker
                .SelectMany(pair => pair.Value)
                .DistinctBy(pair => pair.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            var nonSyntheticBackFillReturns = await GetReturns(dedupedSyntheticConstituentDailyPricesByTicker);

            var backFillReturnsByTickerBySyntheticTicker = new Dictionary<string, Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>>();
            var syntheticReturnsBySyntheticTicker = new Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>();

            foreach (var (syntheticTicker, backFillTickers) in constituentTickersBySyntheticTicker)
            {
                var backFillTickerReturns = new Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>();

                foreach (var backFillTicker in backFillTickers)
                {
                    backFillTickerReturns[backFillTicker] = backFillTicker.StartsWith('$')
                        ? await GetReturnsByPeriodType(backFillTicker)
                        : nonSyntheticBackFillReturns[backFillTicker];
                }

                backFillReturnsByTickerBySyntheticTicker[syntheticTicker] = backFillTickerReturns;
            }

            foreach (var (syntheticTicker, backFillReturnsByTickerByPeriodType) in backFillReturnsByTickerBySyntheticTicker)
            {
                syntheticReturnsBySyntheticTicker[syntheticTicker] = new Dictionary<PeriodType, PeriodReturn[]?>();

                foreach (var periodType in Enum.GetValues<PeriodType>())
                {
                    var backFillReturnsForPeriod = backFillReturnsByTickerByPeriodType.Values
                        .Select(returns => returns[periodType])
                        .Where(returns => returns != null)
                        .ToList();

                    if (backFillReturnsForPeriod.Count == 0)
                    {
                        continue;
                    }

                    var collatedReturns = CollateBackFillReturns(backFillReturnsForPeriod.Select(returns => returns!.ToList()).ToArray()).ToArray();

                    syntheticReturnsBySyntheticTicker[syntheticTicker][periodType] = collatedReturns;

                    await returnRepository.Put(syntheticTicker, collatedReturns, periodType);
                }
            }

            return syntheticReturnsBySyntheticTicker;
        }

        private static IEnumerable<PeriodReturn> CollateBackFillReturns(List<PeriodReturn>[] backfillReturns)
        {
            var collatedReturns = backfillReturns
                .Select((returns, index) =>
                    (returns, nextStartDate: index < backfillReturns.Length - 1
                        ? backfillReturns[index + 1]?.First().PeriodStart
                        : DateTime.MaxValue
                    )
                )
                .SelectMany(item => item.returns.TakeWhile(pair => pair.PeriodStart < item.nextStartDate));

            return collatedReturns;
        }

        public async Task PutSyntheticReturnsInReturnsRepository()
        {
            var synReturnsByTicker = await Task.WhenAll(
                returnRepository.GetSyntheticMonthlyReturns(),
                returnRepository.GetSyntheticYearlyReturns()
            );

            var (putMonthlyTask, putYearlyTask) = (
                synReturnsByTicker[0].Select(r => returnRepository.Put(r.Key, r.Value, PeriodType.Monthly)),
                synReturnsByTicker[1].Select(r => returnRepository.Put(r.Key, r.Value, PeriodType.Yearly)));

            await Task.WhenAll([.. putMonthlyTask, .. putYearlyTask]);
        }

        public async Task<Dictionary<PeriodType, PeriodReturn[]?>> GetReturns(string ticker, IEnumerable<QuotePrice> dailyPriceHistory)
        {
            ArgumentNullException.ThrowIfNull(ticker);
            var periodTypes = Enum.GetValues<PeriodType>();

            return await periodTypes
                .ToAsyncEnumerable()
                .SelectAwait(async periodType => (periodType, returns: await GetReturns(ticker, dailyPriceHistory, periodType)))
                .ToDictionaryAsync(pair => pair.periodType, pair => pair.returns);
        }

        private async Task<PeriodReturn[]?> GetReturns(string ticker, IEnumerable<QuotePrice> dailyPriceHistory, PeriodType periodType)
        {
            var returns = GetPeriodReturns(ticker, dailyPriceHistory.ToList(), periodType);

            if (returns.Count == 0)
            {
                logger.LogWarning("{ticker} has no computable return history for {periodType}", ticker, periodType);

                return null;
            }

            await returnRepository.Put(ticker, returns, periodType);

            return [.. returns];
        }

        private async Task<PeriodReturn[]> GetReturns(string ticker, PeriodType periodType)
        {
            if (!returnRepository.Has(ticker, periodType))
            {
                return null!;
            }

            return [.. await returnRepository.Get(ticker, periodType)];
        }
        private static List<PeriodReturn> GetPeriodReturns(string ticker, IEnumerable<QuotePrice> dailyPrices, PeriodType periodType)
        {
            var groupedPrices = periodType switch
            {
                PeriodType.Daily => dailyPrices,
                PeriodType.Monthly => GroupPricesByPeriod(dailyPrices, d => new { d.Year, d.Month }),
                PeriodType.Yearly => GroupPricesByPeriod(dailyPrices, d => d.Year),
                _ => throw new NotImplementedException()
            };

            var periodReturns = CalculateReturns(ticker, groupedPrices, periodType);

            return periodType switch
            {
                PeriodType.Daily => periodReturns,
                PeriodType.Monthly => GetPeriodOnlyReturns(periodReturns, d => new DateTime(d.Year, d.Month, 1)),
                PeriodType.Yearly => GetPeriodOnlyReturns(periodReturns, d => new DateTime(d.Year, 1, 1)),
                _ => throw new NotImplementedException()
            };
        }

        private static List<PeriodReturn> GetPeriodOnlyReturns(List<PeriodReturn> returns, Func<DateTime, DateTime> adjustDate)
            => returns.Select(r => r with { PeriodStart = adjustDate(r.PeriodStart) }).ToList();

        private static List<QuotePrice> GroupPricesByPeriod<TKey>(IEnumerable<QuotePrice> prices, Func<DateTime, TKey> keySelector)
            => prices
                .GroupBy(r => keySelector(r.DateTime))
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

        private static List<PeriodReturn> CalculateReturns(string ticker, IEnumerable<QuotePrice> prices, PeriodType periodType)
        {
            static decimal CalculateChange(decimal x, decimal y) => (y - x) / x * 100m;

            return prices
                .Zip(prices.Skip(1), (prev, current) => new PeriodReturn
                {
                    PeriodStart = current.DateTime,
                    ReturnPercentage = CalculateChange(prev.AdjustedClose, current.AdjustedClose),
                    SourceTicker = ticker,
                    PeriodType = periodType
                })
                .ToList();
        }
    }
}
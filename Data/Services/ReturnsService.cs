using Data.Models;
using Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Data.Services
{
    internal class ReturnsService(IReturnRepository returnRepository, ILogger<ReturnsService> logger) : IReturnsService
    {
        public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(
            Dictionary<string, IEnumerable<QuotePrice>> dailyPricesByTicker)
        {
            ArgumentNullException.ThrowIfNull(dailyPricesByTicker);

            var returnTasks = dailyPricesByTicker.Select(pair => GetReturns(pair.Key, pair.Value));
            var returnResults = await Task.WhenAll(returnTasks);
            var returnsByTicker = dailyPricesByTicker.Keys
                .Zip(returnResults, (ticker, returns) => (ticker, returns))
                .ToDictionary(pair => pair.ticker, pair => pair.returns);

            return returnsByTicker;
        }

        public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetSyntheticReturns(
            HashSet<string> syntheticTickers)
        {
            ArgumentNullException.ThrowIfNull(syntheticTickers);

            var syntheticReturns = await syntheticTickers.ToAsyncEnumerable()
                .ToDictionaryAwaitAsync(
                    async ticker => ticker,
                    async ticker => await Enum.GetValues<PeriodType>().ToAsyncEnumerable()
                        .ToDictionaryAwaitAsync(
                            async periodType => periodType,
                            async periodType => !returnRepository.Has(ticker, periodType)
                                ? null
                                : (await returnRepository.Get(ticker, periodType)).ToArray()
                        )
                );

            return syntheticReturns;
        }

        public async Task RefreshSyntheticReturns()
        {
            var synReturnsByTicker = await Task.WhenAll(
                returnRepository.GetSyntheticMonthlyReturns(),
                returnRepository.GetSyntheticYearlyReturns()
            );

            var synMonthlyReturnsPutTasks = synReturnsByTicker[0].Select(r
                => returnRepository.Put(r.Key, r.Value, PeriodType.Monthly));

            var synYearlyReturnsPutTasks = synReturnsByTicker[1].Select(r
                => returnRepository.Put(r.Key, r.Value, PeriodType.Yearly));

            await Task.WhenAll(synMonthlyReturnsPutTasks.Concat(synYearlyReturnsPutTasks));
        }

        public async Task<Dictionary<PeriodType, PeriodReturn[]>> GetReturns(string ticker, IEnumerable<QuotePrice> dailyPriceHistory)
        {
            ArgumentNullException.ThrowIfNull(ticker);
            var periodTypes = Enum.GetValues<PeriodType>();

            return await periodTypes
                .ToAsyncEnumerable()
                .SelectAwait(async periodType => (periodType, returns: await GetReturns(ticker, dailyPriceHistory, periodType)))
                .ToDictionaryAsync(pair => pair.periodType, pair => pair.returns);
        }

        private async Task<PeriodReturn[]> GetReturns(string ticker, IEnumerable<QuotePrice> dailyPriceHistory, PeriodType periodType)
        {
            var returns = GetPeriodReturns(ticker, dailyPriceHistory.ToList(), periodType);

            if (returns.Count == 0)
            {
                logger.LogWarning("{ticker} has no computable return history for {periodType}", ticker, periodType);
            }
            else
            {
                await returnRepository.Put(ticker, returns, periodType);
            }

            return [.. returns];
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
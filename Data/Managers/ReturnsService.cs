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

        private async Task<Dictionary<PeriodType, PeriodReturn[]>> GetReturns(
            string ticker,
            IEnumerable<QuotePrice> dailyPriceHistory)
        {
            var periodTypes = Enum.GetValues<PeriodType>().ToList();
            var returnTasks = periodTypes.Select(periodType => GetReturnsForPeriod(ticker, dailyPriceHistory, periodType));
            var returnResults = await Task.WhenAll(returnTasks);
            var returnsByPeriodType = periodTypes
                .Zip(returnResults, (periodType, returns) => (periodType, returns))
                .ToDictionary(pair => pair.periodType, pair => pair.returns);

            return returnsByPeriodType;
        }

        private async Task<PeriodReturn[]> GetReturnsForPeriod(
            string ticker,
            IEnumerable<QuotePrice> dailyPriceHistory,
            PeriodType periodType)
        {
            var returns = periodType switch
            {
                PeriodType.Daily => GetPeriodReturns(dailyPriceHistory, ticker, periodType, p => p),
                PeriodType.Monthly => GetPeriodReturns(dailyPriceHistory, ticker, periodType, GroupByMonth),
                PeriodType.Yearly => GetPeriodReturns(dailyPriceHistory, ticker, periodType, GroupByYear),
                _ => throw new NotImplementedException()
            };

            await returnRepository.Put(ticker, returns, periodType);

            return [.. returns];
        }

        private static IEnumerable<QuotePrice> GroupByMonth(IEnumerable<QuotePrice> prices)
            => GroupByPeriod(prices, p => new { p.DateTime.Year, p.DateTime.Month });

        private static IEnumerable<QuotePrice> GroupByYear(IEnumerable<QuotePrice> prices)
            => GroupByPeriod(prices, p => p.DateTime.Year);

        private static IEnumerable<QuotePrice> GroupByPeriod<TKey>(
            IEnumerable<QuotePrice> prices,
            Func<QuotePrice, TKey> keySelector)
            => prices.GroupBy(keySelector)
                .Select(g => g.OrderByDescending(p => p.DateTime).First())
                .OrderBy(p => p.DateTime);

        private static PeriodReturn[] GetPeriodReturns(
            IEnumerable<QuotePrice> prices,
            string ticker,
            PeriodType periodType,
            Func<IEnumerable<QuotePrice>, IEnumerable<QuotePrice>> groupingFunction)
        {
            var groupedPrices = groupingFunction(prices).ToArray();
            return CalculateReturns(groupedPrices, ticker, periodType);
        }

        private static PeriodReturn[] CalculateReturns(QuotePrice[] prices, string ticker, PeriodType periodType)
        {
            static decimal CalculateChange(decimal x, decimal y) => (y - x) / x * 100m;

            // Skip the first before it's not relative to the (adjusted) close of the date-before-it's close
            var periodReturns = prices.Zip(prices.Skip(1), (prev, current) => new PeriodReturn
            {
                PeriodStart = current.DateTime,
                ReturnPercentage = CalculateChange(prev.AdjustedClose, current.AdjustedClose),
                SourceTicker = ticker,
                PeriodType = periodType
            });

            return [.. periodReturns];
        }
    }
}
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
            ArgumentNullException.ThrowIfNull(ticker);

            var periodTypes = Enum.GetValues<PeriodType>().ToList();
            var returnTasks = periodTypes.Select(periodType => GetReturns(ticker, dailyPriceHistory, periodType));
            var returnResults = await Task.WhenAll(returnTasks);
            var returnsByPeriodType = periodTypes
                .Zip(returnResults, (periodType, returns) => (periodType, returns))
                .ToDictionary(pair => pair.periodType, pair => pair.returns);

            return returnsByPeriodType;
        }
        private async Task<PeriodReturn[]> GetReturns(
            string ticker,
            IEnumerable<QuotePrice> dailyPriceHistory,
            PeriodType periodType)
        {
            var returns = periodType switch
            {
                PeriodType.Daily => GetDailyReturns(ticker, dailyPriceHistory.ToList()),
                PeriodType.Monthly => GetMonthlyReturns(ticker, dailyPriceHistory.ToList()),
                PeriodType.Yearly => GetYearlyReturns(ticker, dailyPriceHistory.ToList()),
                _ => throw new NotImplementedException()
            };

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

        private static List<PeriodReturn> GetDailyReturns(string ticker, List<QuotePrice> dailyPrices)
        {
            return CalculateReturns(ticker, dailyPrices, PeriodType.Daily);
        }

        // TODO test
        private static List<PeriodReturn> GetMonthlyReturns(string ticker, List<QuotePrice> dailyPrices)
        {
            var monthlyCloses = dailyPrices
                .GroupBy(r => new { r.DateTime.Year, r.DateTime.Month })
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

            var monthlyReturns = CalculateReturns(ticker, monthlyCloses, PeriodType.Monthly);

            return monthlyReturns
                .Select(r => new PeriodReturn()
                {
                    PeriodStart = new DateTime(r.PeriodStart.Year, r.PeriodStart.Month, 1),
                    ReturnPercentage = r.ReturnPercentage,
                    SourceTicker = r.SourceTicker,
                    PeriodType = r.PeriodType,
                })
                .ToList();
        }

        // TODO test
        private static List<PeriodReturn> GetYearlyReturns(string ticker, List<QuotePrice> dailyPrices)
        {
            var yearlyCloses = dailyPrices
                .GroupBy(r => r.DateTime.Year)
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

            var yearlyReturns = CalculateReturns(ticker, yearlyCloses, PeriodType.Yearly);

            return yearlyReturns
                .Select(r => new PeriodReturn()
                {
                    PeriodStart = new DateTime(r.PeriodStart.Year, 1, 1),
                    ReturnPercentage = r.ReturnPercentage,
                    SourceTicker = r.SourceTicker,
                    PeriodType = r.PeriodType
                })
                .ToList();
        }

        private static List<PeriodReturn> CalculateReturns(string ticker, List<QuotePrice> prices, PeriodType periodType)
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
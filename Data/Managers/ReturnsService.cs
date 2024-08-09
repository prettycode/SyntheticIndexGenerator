using Data.Models;
using Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Data.Controllers
{
    internal class ReturnsService(IQuoteRepository quoteRepository, IReturnRepository returnRepository, ILogger<ReturnsService> logger) : IReturnsService
    {
        private IQuoteRepository QuoteCache { get; init; } = quoteRepository;

        private IReturnRepository ReturnCache { get; init; } = returnRepository;

        private ILogger<ReturnsService> Logger { get; init; } = logger;

        public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(
            Dictionary<string, IEnumerable<QuotePrice>> quotesByTicker)
        {
            ArgumentNullException.ThrowIfNull(quotesByTicker);

            var returnTasks = quotesByTicker.Select(pair => GetReturns(pair.Key, pair.Value));
            var returnResults = await Task.WhenAll(returnTasks);
            var returnsByTicker = quotesByTicker.Keys
                .Zip(returnResults, (ticker, returns) => (ticker, returns))
                .ToDictionary(pair => pair.ticker, pair => pair.returns);

            return returnsByTicker;
        }

        public async Task RefreshSyntheticReturns()
        {
            var synReturnsByTicker = await Task.WhenAll(
                ReturnCache.GetSyntheticMonthlyReturns(),
                ReturnCache.GetSyntheticYearlyReturns()
            );

            var synMonthlyReturnsPutTasks = synReturnsByTicker[0].Select(r => ReturnCache.Put(r.Key, r.Value, PeriodType.Monthly));
            var synYearlyReturnsPutTasks = synReturnsByTicker[1].Select(r => ReturnCache.Put(r.Key, r.Value, PeriodType.Yearly));

            await Task.WhenAll(synMonthlyReturnsPutTasks.Concat(synYearlyReturnsPutTasks));
        }

        // TODO needs to take in prices and put in cache before returning date-filtered cache results.
        public Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime startDate, DateTime endDate)
        {
            return ReturnCache.Get(ticker, period, startDate, endDate);
        }

        private async Task<Dictionary<PeriodType, PeriodReturn[]>> GetReturns(
            string ticker,
            IEnumerable<QuotePrice> priceHistory)
        {
            var periodTypes = Enum.GetValues<PeriodType>().ToList();
            var returnTasks = periodTypes.Select(periodType => GetReturns(ticker, priceHistory, periodType));
            var returnResults = await Task.WhenAll(returnTasks);
            var returnsByPeriodType = periodTypes
                .Zip(returnResults, (periodType, returns) => (periodType, returns))
                .ToDictionary(pair => pair.periodType, pair => pair.returns);

            return returnsByPeriodType;
        }

        private async Task<PeriodReturn[]> GetReturns(
            string ticker,
            IEnumerable<QuotePrice> priceHistory,
            PeriodType periodType)
        {
            var returns = periodType switch
            {
                PeriodType.Daily => GetDailyReturns(ticker, priceHistory),
                PeriodType.Monthly => GetMonthlyReturns(ticker, priceHistory),
                PeriodType.Yearly => GetYearlyReturns(ticker, priceHistory),
                _ => throw new NotImplementedException()
            };

            await ReturnCache.Put(ticker, returns, periodType);

            return [.. returns];
        }

        private static List<PeriodReturn> GetDailyReturns(string ticker, IEnumerable<QuotePrice> dailyPrices)
        {
            return GetReturns(dailyPrices.ToArray(), ticker, PeriodType.Daily);
        }

        // TODO test
        private static List<PeriodReturn> GetMonthlyReturns(string ticker, IEnumerable<QuotePrice> dailyPrices)
        {
            var monthlyCloses = dailyPrices
                .GroupBy(r => new { r.DateTime.Year, r.DateTime.Month })
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToArray();

            var monthlyReturns = GetReturns(monthlyCloses, ticker, PeriodType.Monthly);

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
        private static List<PeriodReturn> GetYearlyReturns(string ticker, IEnumerable<QuotePrice> dailyPrices)
        {
            var yearlyCloses = dailyPrices
                .GroupBy(r => r.DateTime.Year)
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToArray();

            var yearlyReturns = GetReturns(yearlyCloses, ticker, PeriodType.Yearly);

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

        // TODO test
        private static List<PeriodReturn> GetReturns(QuotePrice[] prices, string ticker, PeriodType returnPeriod, bool skipFirst = true)
        {
            static decimal calculateChange(decimal x, decimal y) => (y - x) / x * 100m;
            static decimal endingPrice(QuotePrice record) => record.AdjustedClose;

            List<PeriodReturn> returns = skipFirst
                ? []
                : [
                    new PeriodReturn()
                    {
                        PeriodStart = prices[0].DateTime,
                        ReturnPercentage = calculateChange(prices[0].Open, endingPrice(prices[0])),
                        SourceTicker = ticker,
                        PeriodType = returnPeriod
                    }
                ];

            for (int i = 1; i < prices.Length; i++)
            {
                var currentDate = prices[i].DateTime;
                var previousDate = prices[i - 1].DateTime;

                if (currentDate <= previousDate)
                {
                    throw new ArgumentException("Collection is out-of-order.", nameof(prices));
                }

                var currentEndPrice = endingPrice(prices[i]);
                var currentStartPrice = endingPrice(prices[i - 1]);

                returns.Add(new PeriodReturn()
                {
                    PeriodStart = currentDate,
                    ReturnPercentage = calculateChange(currentStartPrice, currentEndPrice),
                    SourceTicker = ticker,
                    PeriodType = returnPeriod
                });
            }

            return returns;
        }
    }
}
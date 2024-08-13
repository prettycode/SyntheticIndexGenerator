using Data.Quotes;
using Microsoft.Extensions.Logging;

namespace Data.Returns
{
    internal class ReturnsService : IReturnsService
    {
        private readonly IQuotesService quotesService;
        private readonly IReturnRepository returnRepository;
        private readonly ILogger<ReturnsService> logger;

        public ReturnsService(IQuotesService quotesService, IReturnRepository returnRepository, ILogger<ReturnsService> logger)
        {
            ArgumentNullException.ThrowIfNull(nameof(quotesService));
            ArgumentNullException.ThrowIfNull(nameof(returnRepository));
            ArgumentNullException.ThrowIfNull(nameof(logger));

            this.quotesService = quotesService;
            this.returnRepository = returnRepository;
            this.logger = logger;

            PutSyntheticReturnsInReturnsRepository().ConfigureAwait(false);
        }

        public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(
            HashSet<string> tickers, bool skipRefresh = false)
        {
            ArgumentNullException.ThrowIfNull(tickers);

            var dailyPricesByTicker = await quotesService.GetPrices(tickers, skipRefresh);

            return await dailyPricesByTicker
                .ToAsyncEnumerable()
                .ToDictionaryAwaitAsync(
                    keySelector: pair => ValueTask.FromResult(pair.Key),
                    elementSelector: async pair => await GetReturns(pair.Key, pair.Value)
                );
        }

        public Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime startDate, DateTime endDate)
        {
            return returnRepository.Get(ticker, period, startDate, endDate);
        }

        public async Task PutSyntheticReturnsInReturnsRepository()
        {
            var monthlyReturnsTask = returnRepository.GetSyntheticMonthlyReturns();
            var yearlyReturnsTask = returnRepository.GetSyntheticYearlyReturns();

            await Task.WhenAll(monthlyReturnsTask, yearlyReturnsTask);

            var synReturnsByTickerByPeriodType = new Dictionary<PeriodType, Dictionary<string, List<PeriodReturn>>>
            {
                { PeriodType.Monthly, monthlyReturnsTask.Result },
                { PeriodType.Yearly, yearlyReturnsTask.Result }
            };

            var allPutTasks = synReturnsByTickerByPeriodType
                .SelectMany(kvp => kvp.Value.Select(innerKvp =>
                    returnRepository.Put(innerKvp.Key, innerKvp.Value, kvp.Key)))
                .ToList();

            await Task.WhenAll(allPutTasks);
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
                    Ticker = ticker,
                    PeriodStart = current.DateTime,
                    ReturnPercentage = CalculateChange(prev.AdjustedClose, current.AdjustedClose),
                    PeriodType = periodType
                })
                .ToList();
        }
    }
}
using Data.Quotes;
using Microsoft.Extensions.Logging;

namespace Data.Returns;

internal class ReturnsService : IReturnsService
{
    private readonly IQuotesService quotesService;
    private readonly IReturnRepository returnRepository;
    private readonly ILogger<ReturnsService> logger;

    public ReturnsService(
        IQuotesService quotesService,
        IReturnRepository returnRepository,
        ILogger<ReturnsService> logger)
    {
        ArgumentNullException.ThrowIfNull(nameof(quotesService));
        ArgumentNullException.ThrowIfNull(nameof(returnRepository));
        ArgumentNullException.ThrowIfNull(nameof(logger));

        this.quotesService = quotesService;
        this.returnRepository = returnRepository;
        this.logger = logger;
    }

    public Task<List<PeriodReturn>> GetReturnsHistory(string ticker, PeriodType period, DateTime startDate, DateTime endDate)
    {
        return returnRepository.Get(ticker, period, startDate, endDate);
    }

    public async Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(
        HashSet<string> tickers,
        bool skipRefresh = false)
    {
        async Task<Dictionary<PeriodType, PeriodReturn[]>> ReturnsByPeriodType(
            string ticker,
            IEnumerable<QuotePrice> dailyPriceHistory)
        {
            ArgumentNullException.ThrowIfNull(ticker);
            var periodTypes = Enum.GetValues<PeriodType>();

            return await periodTypes
                .ToAsyncEnumerable()
                .SelectAwait(async periodType
                    => (periodType, returns: await GetReturnsForPeriodType(ticker, dailyPriceHistory, periodType)))
                .ToDictionaryAsync(pair => pair.periodType, pair => pair.returns);
        }

        ArgumentNullException.ThrowIfNull(tickers);

        var dailyPricesByTicker = await quotesService.GetPrices(tickers, skipRefresh);

        return await dailyPricesByTicker
            .ToAsyncEnumerable()
            .ToDictionaryAwaitAsync(
                keySelector: pair => ValueTask.FromResult(pair.Key),
                elementSelector: async pair => await ReturnsByPeriodType(pair.Key, pair.Value)
            );
    }

    private async Task<PeriodReturn[]> GetReturnsForPeriodType(
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
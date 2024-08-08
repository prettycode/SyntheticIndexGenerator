using Data.Models;
using Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Data.Controllers
{
    public class ReturnsManager(IQuoteRepository quoteRepository, IReturnRepository returnRepository, ILogger<ReturnsManager> logger)
    {
        private IQuoteRepository QuoteCache { get; init; } = quoteRepository;

        private IReturnRepository ReturnCache { get; init; } = returnRepository;

        private ILogger<ReturnsManager> Logger { get; init; } = logger;

        public async Task RefreshReturn(string ticker)
        {
            ArgumentNullException.ThrowIfNull(ticker);

            var history = await QuoteCache.Get(ticker);
            var priceHistory = history.Prices;

            await Task.WhenAll(
                ReturnCache.Put(ticker, GetDailyReturns(ticker, priceHistory), PeriodType.Daily),
                ReturnCache.Put(ticker, GetMonthlyReturns(ticker, priceHistory), PeriodType.Monthly),
                ReturnCache.Put(ticker, GetYearlyReturns(ticker, priceHistory), PeriodType.Yearly)
            );
        }

        public async Task RefreshReturns()
        {
            var tickers = QuoteCache.GetAllTickers();
            var tickerRefreshTasks = tickers.Select(ticker => RefreshReturn(ticker));
            var syntheticRefreshTasks = RefreshSyntheticReturns();

            await Task.WhenAll([syntheticRefreshTasks, .. tickerRefreshTasks]);
        }

        private async Task RefreshSyntheticReturns()
        {
            var synReturnsByTicker = await Task.WhenAll(
                ReturnCache.GetSyntheticMonthlyReturns(),
                ReturnCache.GetSyntheticYearlyReturns()
            );

            var synMonthlyReturnsPutTasks = synReturnsByTicker[0].Select(r => ReturnCache.Put(r.Key, r.Value, PeriodType.Monthly));
            var synYearlyReturnsPutTasks = synReturnsByTicker[1].Select(r => ReturnCache.Put(r.Key, r.Value, PeriodType.Yearly));

            await Task.WhenAll(synMonthlyReturnsPutTasks.Concat(synYearlyReturnsPutTasks));
        }

        private static List<PeriodReturn> GetDailyReturns(string ticker, List<QuotePrice> dailyPrices)
        {
            return GetReturns(dailyPrices, ticker, PeriodType.Daily);
        }

        // TODO test
        private static List<PeriodReturn> GetMonthlyReturns(string ticker, List<QuotePrice> dailyPrices)
        {
            var monthlyCloses = dailyPrices
                .GroupBy(r => new { r.DateTime.Year, r.DateTime.Month })
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

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
        private static List<PeriodReturn> GetYearlyReturns(string ticker, List<QuotePrice> dailyPrices)
        {
            var yearlyCloses = dailyPrices
                .GroupBy(r => r.DateTime.Year)
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

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
        private static List<PeriodReturn> GetReturns(List<QuotePrice> prices, string ticker, PeriodType returnPeriod, bool skipFirst = true)
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

            for (int i = 1; i < prices.Count; i++)
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
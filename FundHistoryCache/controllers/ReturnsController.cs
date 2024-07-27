using FundHistoryCache.Models;
using FundHistoryCache.Repositories;

namespace FundHistoryCache.Controllers
{
    public static class ReturnsController
    {
        public static Task RefreshReturns(QuoteRepository quotesCache, ReturnsRepository returnsCache)
        {
            ArgumentNullException.ThrowIfNull(quotesCache);
            ArgumentNullException.ThrowIfNull(returnsCache);

            async Task<Task> refreshSyntheticReturns()
            {
                var synReturnsByTicker = await Task.WhenAll([
                    returnsCache.GetSyntheticMonthlyReturns(),
                    returnsCache.GetSyntheticYearlyReturns()
                ]);

                var synMonthlyReturnsPutTasks = synReturnsByTicker[0].Select(r => returnsCache.Put(r.Key, r.Value, ReturnPeriod.Monthly));
                var synYearlyReturnsPutTasks = synReturnsByTicker[1].Select(r => returnsCache.Put(r.Key, r.Value, ReturnPeriod.Yearly));

                return Task.WhenAll([
                    .. synMonthlyReturnsPutTasks,
                    .. synYearlyReturnsPutTasks
                ]);
            }

            IEnumerable<Task> refreshQuoteReturns()
            {
                var tickers = quotesCache.GetAllTickers();

                return tickers.Select(async ticker =>
                {
                    var history = await quotesCache.Get(ticker);
                    var priceHistory = history!.Prices;

                    await Task.WhenAll(
                        returnsCache.Put(ticker, GetDailyReturns(ticker, priceHistory), ReturnPeriod.Daily),
                        returnsCache.Put(ticker, GetMonthlyReturns(ticker, priceHistory), ReturnPeriod.Monthly),
                        returnsCache.Put(ticker, GetYearlyReturns(ticker, priceHistory), ReturnPeriod.Yearly)
                    );
                });
            }

            return Task.WhenAll([
                refreshSyntheticReturns(),
                .. refreshQuoteReturns()
            ]);
        }

        private static List<PeriodReturn> GetDailyReturns(string ticker, List<QuotePriceRecord> dailyPrices)
        {
            return GetReturns(dailyPrices, ticker, ReturnPeriod.Daily);
        }

        private static List<PeriodReturn> GetMonthlyReturns(string ticker, List<QuotePriceRecord> dailyPrices)
        {
            var monthlyCloses = dailyPrices
                .GroupBy(r => new { r.DateTime.Year, r.DateTime.Month })
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

            var monthlyReturns = GetReturns(monthlyCloses, ticker, ReturnPeriod.Monthly);

            return monthlyReturns
                .Select(r => new PeriodReturn(new DateTime(r.PeriodStart.Year, r.PeriodStart.Month, 1), r.ReturnPercentage, r.SourceTicker!, r.ReturnPeriod))
                .ToList();
        }

        private static List<PeriodReturn> GetYearlyReturns(string ticker, List<QuotePriceRecord> dailyPrices)
        {
            var yearlyCloses = dailyPrices
                .GroupBy(r => r.DateTime.Year)
                .Select(g => g.OrderByDescending(r => r.DateTime).First())
                .OrderBy(r => r.DateTime)
                .ToList();

            var yearlyReturns = GetReturns(yearlyCloses, ticker, ReturnPeriod.Yearly);

            return yearlyReturns
                .Select(r => new PeriodReturn(new DateTime(r.PeriodStart.Year, 1, 1), r.ReturnPercentage, r.SourceTicker!, r.ReturnPeriod))
                .ToList();
        }

        private static List<PeriodReturn> GetReturns(List<QuotePriceRecord> prices, string ticker, ReturnPeriod returnPeriod, bool skipFirst = true)
        {
            static decimal calcChange(decimal x, decimal y) => (y - x) / x * 100m;
            static decimal endingPrice(QuotePriceRecord record) => record.AdjustedClose;

            List<PeriodReturn> returns = skipFirst
                ? []
                : [
                    new(prices[0].DateTime, calcChange(prices[0].Open, endingPrice(prices[0])), ticker, returnPeriod)
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

                returns.Add(new(currentDate, calcChange(currentStartPrice, currentEndPrice), ticker, returnPeriod));
            }

            return returns;
        }
    }
}
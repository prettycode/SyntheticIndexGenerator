public static class ReturnsController
{
    public static Task RefreshReturns(QuoteRepository quotesCache, ReturnsRepository returnsCache)
    {
        ArgumentNullException.ThrowIfNull(quotesCache);
        ArgumentNullException.ThrowIfNull(returnsCache);

        async Task<Task> refreshSyntheticReturns()
        {
            var indexReturnsByTicker = await returnsCache.GetSyntheticMonthlyReturns();
            var putReturnsTasks = indexReturnsByTicker.Select(r => returnsCache.Put(r.Key, r.Value, ReturnPeriod.Monthly));

            return Task.WhenAll(putReturnsTasks);
        }

        IEnumerable<Task> refreshQuoteReturns()
        {
            var tickers = quotesCache.GetAllTickers();

            return tickers.Select(async ticker =>
            {
                var history = await quotesCache.Get(ticker);
                var priceHistory = history!.Prices.ToList();

                await Task.WhenAll(
                    returnsCache.Put(ticker, ReturnsController.GetDailyReturns(priceHistory), ReturnPeriod.Daily),
                    returnsCache.Put(ticker, ReturnsController.GetMonthReturns(priceHistory), ReturnPeriod.Monthly),
                    returnsCache.Put(ticker, ReturnsController.GetYearlyReturns(priceHistory), ReturnPeriod.Yearly)
                );
            });
        }

        return Task.WhenAll([
            refreshSyntheticReturns(),
            .. refreshQuoteReturns()
        ]);
    }

    private static List<PeriodReturn> GetDailyReturns(List<QuotePriceRecord> dailyPrices)
    {
        return ReturnsController.GetReturns(dailyPrices);
    }

    private static List<PeriodReturn> GetMonthReturns(List<QuotePriceRecord> dailyPrices)
    {
        var monthlyCloses = dailyPrices
            .GroupBy(r => new { r.DateTime.Year, r.DateTime.Month })
            .Select(g => g.OrderByDescending(r => r.DateTime).First())
            .OrderBy(r => r.DateTime)
            .ToList();

        var monthlyReturns = ReturnsController.GetReturns(monthlyCloses);

        return monthlyReturns
            .Select(r => new PeriodReturn(new DateTime(r.PeriodStart.Year, r.PeriodStart.Month, 1), r.ReturnPercentage))
            .ToList();
    }

    private static List<PeriodReturn> GetYearlyReturns(List<QuotePriceRecord> dailyPrices)
    {
        var yearlyCloses = dailyPrices
            .GroupBy(r => r.DateTime.Year)
            .Select(g => g.OrderByDescending(r => r.DateTime).First())
            .OrderBy(r => r.DateTime)
            .ToList();

        var yearlyReturns = ReturnsController.GetReturns(yearlyCloses);

        return yearlyReturns
            .Select(r => new PeriodReturn(new DateTime(r.PeriodStart.Year, 1, 1), r.ReturnPercentage))
            .ToList();
    }

    private static List<PeriodReturn> GetReturns(List<QuotePriceRecord> prices, bool skipFirst = true)
    {
        static decimal calcChange(decimal x, decimal y) => (y - x) / x * 100m;
        static decimal endingPrice(QuotePriceRecord record) => record.AdjustedClose;

        List<PeriodReturn> returns = skipFirst
            ? []
            : [
                new(prices[0].DateTime, calcChange(prices[0].Open, endingPrice(prices[0])))
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

            returns.Add(new(currentDate, calcChange(currentStartPrice, currentEndPrice)));
        }

        return returns;
    }
}
public static class FundHistoryPeriodReturns
{
    public static List<KeyValuePair<DateTime, decimal>> GetDailyReturns(List<PriceRecord> dailyPrices)
    {
        return FundHistoryPeriodReturns.GetReturns(dailyPrices);
    }

    public static List<KeyValuePair<DateTime, decimal>> GetMonthReturns(List<PriceRecord> dailyPrices)
    {
        var monthlyCloses = dailyPrices
            .GroupBy(r => new { r.DateTime.Year, r.DateTime.Month })
            .Select(g => g.OrderByDescending(r => r.DateTime).First())
            .OrderBy(r => r.DateTime)
            .ToList();

        var monthlyReturns = FundHistoryPeriodReturns.GetReturns(monthlyCloses);

        return monthlyReturns
            .Select(r => new KeyValuePair<DateTime, decimal>(new DateTime(r.Key.Year, r.Key.Month, 1), r.Value))
            .ToList();
    }

    public static List<KeyValuePair<DateTime, decimal>> GetYearlyReturns(List<PriceRecord> dailyPrices)
    {
        var yearlyCloses = dailyPrices
            .GroupBy(r => r.DateTime.Year)
            .Select(g => g.OrderByDescending(r => r.DateTime).First())
            .OrderBy(r => r.DateTime)
            .ToList();

        var yearlyReturns = FundHistoryPeriodReturns.GetReturns(yearlyCloses);

        return yearlyReturns
            .Select(r => new KeyValuePair<DateTime, decimal>(new DateTime(r.Key.Year, 1, 1), r.Value))
            .ToList();
    }

    private static List<KeyValuePair<DateTime, decimal>> GetReturns(List<PriceRecord> prices, bool skipFirst = true)
    {
        static decimal calcChange(decimal x, decimal y) => (y - x) / x * 100m;
        static decimal endingPrice(PriceRecord record) => record.AdjustedClose;

        List<KeyValuePair<DateTime, decimal>> returns = skipFirst
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


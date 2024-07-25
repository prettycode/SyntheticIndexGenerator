public static class ReturnsController
{
    private enum TimePeriod
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public static async Task RefreshReturns(QuoteRepository cache, string savePath)
    {
        var tickers = cache.GetCacheKeys();

        await Task.WhenAll(tickers.Select(async ticker =>
        {
            var history = await cache.Get(ticker);
            var priceHistory = history!.Prices.ToList();

            await Task.WhenAll(
                WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Daily, savePath),
                WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Monthly, savePath),
                WriteFundHistoryReturns(ticker, priceHistory, TimePeriod.Yearly, savePath)
            );
        }));
    }

    private static async Task WriteFundHistoryReturns(string ticker, List<QuotePriceRecord> history, TimePeriod period, string savePath)
    {
        List<KeyValuePair<DateTime, decimal>> returns = period switch
        {
            TimePeriod.Daily => ReturnsController.GetDailyReturns(history),
            TimePeriod.Monthly => ReturnsController.GetMonthReturns(history),
            TimePeriod.Yearly => ReturnsController.GetYearlyReturns(history),
            _ => throw new NotImplementedException()
        };

        string csvFilePath = Path.Combine(savePath, $"./{period.ToString().ToLowerInvariant()}/{ticker}.csv");
        var csvFileLines = returns.Select(r => $"{r.Key:yyyy-MM-dd},{r.Value}");

        await File.WriteAllLinesAsync(csvFilePath, csvFileLines);
    }

    private static List<KeyValuePair<DateTime, decimal>> GetDailyReturns(List<QuotePriceRecord> dailyPrices)
    {
        return ReturnsController.GetReturns(dailyPrices);
    }

    private static List<KeyValuePair<DateTime, decimal>> GetMonthReturns(List<QuotePriceRecord> dailyPrices)
    {
        var monthlyCloses = dailyPrices
            .GroupBy(r => new { r.DateTime.Year, r.DateTime.Month })
            .Select(g => g.OrderByDescending(r => r.DateTime).First())
            .OrderBy(r => r.DateTime)
            .ToList();

        var monthlyReturns = ReturnsController.GetReturns(monthlyCloses);

        return monthlyReturns
            .Select(r => new KeyValuePair<DateTime, decimal>(new DateTime(r.Key.Year, r.Key.Month, 1), r.Value))
            .ToList();
    }

    private static List<KeyValuePair<DateTime, decimal>> GetYearlyReturns(List<QuotePriceRecord> dailyPrices)
    {
        var yearlyCloses = dailyPrices
            .GroupBy(r => r.DateTime.Year)
            .Select(g => g.OrderByDescending(r => r.DateTime).First())
            .OrderBy(r => r.DateTime)
            .ToList();

        var yearlyReturns = ReturnsController.GetReturns(yearlyCloses);

        return yearlyReturns
            .Select(r => new KeyValuePair<DateTime, decimal>(new DateTime(r.Key.Year, 1, 1), r.Value))
            .ToList();
    }

    private static List<KeyValuePair<DateTime, decimal>> GetReturns(List<QuotePriceRecord> prices, bool skipFirst = true)
    {
        static decimal calcChange(decimal x, decimal y) => (y - x) / x * 100m;
        static decimal endingPrice(QuotePriceRecord record) => record.AdjustedClose;

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
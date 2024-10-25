using Microsoft.Extensions.Logging;

namespace Data.Returns;

internal static class ReturnsCalculations
{

    public static IEnumerable<PeriodReturn> CalculateReturnsForPeriodType(
        string ticker,
        IEnumerable<(DateTime date, decimal endingPriceOnDate)> dailyPrices,
        PeriodType periodType)
    {
        static IEnumerable<PeriodReturn> GetPeriodOnlyReturns(IEnumerable<PeriodReturn> returns, Func<DateTime, DateTime> adjustDate)
            => returns.Select(r => r with { PeriodStart = adjustDate(r.PeriodStart) });

        static IEnumerable<(DateTime date, decimal endingPriceOnDate)> GroupPricesByPeriod<TKey>(IEnumerable<(DateTime date, decimal endingPriceOnDate)> prices, Func<DateTime, TKey> keySelector)
            => prices
                .GroupBy(r => keySelector(r.date))
                .Select(g => g.OrderByDescending(r => r.date).First())
                .OrderBy(r => r.date);

        static IEnumerable<PeriodReturn> CalculateReturns(string ticker, IEnumerable<(DateTime date, decimal endingPriceOnDate)> prices, PeriodType periodType)
        {
            static decimal CalculateChange(decimal x, decimal y) => (y - x) / x * 100m;

            return prices
                .Zip(prices.Skip(1), (prev, current) => new PeriodReturn
                {
                    Ticker = ticker,
                    PeriodStart = current.date,
                    ReturnPercentage = CalculateChange(prev.endingPriceOnDate, current.endingPriceOnDate),
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

        return returnsForPeriodType;
    }
}

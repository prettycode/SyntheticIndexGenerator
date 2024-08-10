using Data.Models;

namespace Data.Services
{
    public interface IReturnsService
    {
        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetReturns(
            Dictionary<string, IEnumerable<QuotePrice>> dailyPricesByTicker);

        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetSyntheticReturns(
            HashSet<string> syntheticTickers,
            Dictionary<string, Dictionary<string, IEnumerable<QuotePrice>>> syntheticConstituentDailyPricesByTicker);

        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetReturns(HashSet<string> tickers);

        Task RefreshSyntheticReturns();
    }
}
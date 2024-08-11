using Data.Models;

namespace Data.Services
{
    public interface IReturnsService
    {
        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(
            Dictionary<string, IEnumerable<QuotePrice>> dailyPricesByTicker);

        Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime startDate, DateTime endDate);

        Task PutSyntheticReturnsInReturnsRepository();
    }
}
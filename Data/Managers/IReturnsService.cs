using Data.Models;

namespace Data.Controllers
{
    public interface IReturnsService
    {
        Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime startDate, DateTime endDate);

        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(
            Dictionary<string, IEnumerable<QuotePrice>> quotesByTicker);

        Task RefreshSyntheticReturns();
    }
}
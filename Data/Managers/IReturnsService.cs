using Data.Models;

namespace Data.Controllers
{
    public interface IReturnsService
    {
        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(HashSet<string> tickers);

        Task<PeriodReturn[]> GetReturns(string ticker, PeriodType periodType);

        Task RefreshSyntheticReturns();

        Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime startDate, DateTime endDate);
    }
}
using Data.Models;

namespace Data.Controllers
{
    public interface IReturnsService
    {
        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(
            Dictionary<string, IEnumerable<QuotePrice>> quotesByTicker);

        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetSyntheticReturns(
            HashSet<string> syntheticTickers);

        Task RefreshSyntheticReturns();
    }
}
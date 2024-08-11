using Data.Models;

namespace Data.Services
{
    public interface IReturnsService
    {
        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetQuoteReturns(
            HashSet<string> tickers);

        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]?>>> GetSyntheticIndexReturns(HashSet<string> tickers);

        Task<IEnumerable<PeriodReturn[]>> GetPeriodReturns(HashSet<string> tickers, PeriodType periodType);

        Task PutSyntheticReturnsInReturnsRepository();
    }
}
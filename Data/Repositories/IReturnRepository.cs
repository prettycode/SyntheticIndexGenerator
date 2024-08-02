using Data.Models;

namespace Data.Repositories
{
    public interface IReturnRepository
    {
        Task<List<PeriodReturn>> Get(string ticker, ReturnPeriod period);
        Task<List<PeriodReturn>> Get(string ticker, ReturnPeriod period, DateTime start);
        Task<List<PeriodReturn>> Get(string ticker, ReturnPeriod period, DateTime start, DateTime end);
        Task<Dictionary<string, List<PeriodReturn>>> GetSyntheticMonthlyReturns();
        Task<Dictionary<string, List<PeriodReturn>>> GetSyntheticYearlyReturns();
        bool Has(string ticker, ReturnPeriod period);
        Task Put(string ticker, List<PeriodReturn> returns, ReturnPeriod period);
    }
}
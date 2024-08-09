using Data.Models;

namespace Data.Repositories
{
    internal interface IReturnRepository
    {
        Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime? start = null, DateTime? end = null);

        Task<Dictionary<string, List<PeriodReturn>>> GetSyntheticMonthlyReturns();

        Task<Dictionary<string, List<PeriodReturn>>> GetSyntheticYearlyReturns();

        bool Has(string ticker, PeriodType period);

        Task Put(string ticker, List<PeriodReturn> returns, PeriodType period);
    }
}
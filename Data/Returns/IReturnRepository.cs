namespace Data.Returns
{
    internal interface IReturnRepository
    {
        Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime? start = null, DateTime? end = null);

        bool Has(string ticker, PeriodType period);

        Task Put(string ticker, IEnumerable<PeriodReturn> returns, PeriodType period);
    }
}
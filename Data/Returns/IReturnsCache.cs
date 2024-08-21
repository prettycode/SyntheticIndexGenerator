namespace Data.Returns;

internal interface IReturnsCache
{
    Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime? start = null, DateTime? end = null);

    Task<IEnumerable<PeriodReturn>?> TryGetValue(string ticker, PeriodType period);

    Task<List<PeriodReturn>> Put(string ticker, IEnumerable<PeriodReturn> returns, PeriodType period);
}
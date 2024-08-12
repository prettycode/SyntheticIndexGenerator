namespace Data.Returns
{
    public interface IReturnsService
    {
        Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(HashSet<string> tickers, bool skipRefresh = false);

        Task<List<PeriodReturn>> Get(string ticker, PeriodType period, DateTime startDate, DateTime endDate);

        Task PutSyntheticReturnsInReturnsRepository();
    }
}
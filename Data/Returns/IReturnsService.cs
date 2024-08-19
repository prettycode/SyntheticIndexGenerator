namespace Data.Returns;

public interface IReturnsService
{
    Task<Dictionary<string, Dictionary<PeriodType, PeriodReturn[]>>> GetReturns(HashSet<string> tickers, bool skipRefresh = false);

    Task<List<PeriodReturn>> GetReturnsHistory(string ticker, PeriodType period, DateTime startDate, DateTime endDate);
}
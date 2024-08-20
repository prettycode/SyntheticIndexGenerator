namespace Data.Returns;

public interface IReturnsService
{
    Task<Dictionary<string, List<PeriodReturn>>> GetReturnsHistory(
        HashSet<string> tickers,
        PeriodType periodType,
        DateTime startDate,
        DateTime endDate,
        bool fromCacheOnly = false);

    Task<List<PeriodReturn>> GetReturnsHistory(
        string ticker,
        PeriodType periodType,
        DateTime startDate,
        DateTime endDate,
        bool fromCacheOnly = false);
}
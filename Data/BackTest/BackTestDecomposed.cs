namespace Data.BackTest;

public class BackTestDecomposed
{
    public required Dictionary<string, BackTestPeriodReturn[]> ReturnsByTicker { get; init; }
    public required Dictionary<string, BackTestRebalanceEvent[]> RebalancesByTicker { get; init; }
}


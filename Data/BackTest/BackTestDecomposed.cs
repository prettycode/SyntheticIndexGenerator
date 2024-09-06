namespace Data.BackTest;

public class BackTestDecomposed
{
    public Dictionary<string, BackTestPeriodReturn[]> ReturnsByTicker { get; init; } = [];
    public Dictionary<string, BackTestRebalanceEvent[]> RebalancesByTicker { get; init; } = [];
}


using Data.BackTest;
using Data.Returns;

namespace WebApp.Server.Controllers;

public class BackTestRequest
{
    public required IEnumerable<IEnumerable<BackTestAllocation>> Portfolios { get; init; }

    public decimal? StartingBalance { get; init; }

    public PeriodType? PeriodType { get; init; }

    public DateTime? FirstPeriod { get; init; }

    public DateTime? LastPeriod { get; init; }

    public BackTestRebalanceStrategy? RebalanceStrategy { get; init; }

    public decimal? RebalanceBandThreshold { get; init; }

    public bool? IncludeIncompleteEndingPeriod { get; init; }
}
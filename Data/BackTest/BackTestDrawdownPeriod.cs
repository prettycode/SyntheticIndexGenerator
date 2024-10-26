using Data.Returns;

namespace Data.BackTest;

public readonly struct BackTestDrawdownPeriod(BackTestPeriodReturn currentReturnPeriod)
{
    public required string Ticker { get; init; }

    public DateTime FirstNegativePeriodStart { get; init; }

    public DateTime? FirstPositivePeriodStart { get; init; }

    public decimal MaximumDrawdownPercentage { get; init; }

    public PeriodType PeriodType { get; init; } = currentReturnPeriod.PeriodType;

    public TimeSpan DrawdownTimeSpan
        => (FirstPositivePeriodStart ?? PeriodType switch
        {
            PeriodType.Daily => currentReturnPeriod.PeriodStart.AddDays(1),
            PeriodType.Monthly => currentReturnPeriod.PeriodStart.AddMonths(1),
            PeriodType.Yearly => currentReturnPeriod.PeriodStart.AddYears(1),
            _ => throw new InvalidOperationException()
        })
        - FirstNegativePeriodStart;
}
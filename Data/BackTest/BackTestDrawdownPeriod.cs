using Data.Returns;

namespace Data.BackTest;

public readonly struct BackTestDrawdownPeriod(BackTestPeriodReturn lastReturnPeriod)
{
    public string Ticker { get; init; }

    public DateTime FirstNegativePeriodStart { get; init; }

    public DateTime? FirstPositivePeriodStart { get; init; }

    public PeriodType PeriodType { get; init; } = lastReturnPeriod.PeriodType;

    public TimeSpan DrawdownTimeSpan
        => (FirstPositivePeriodStart ?? PeriodType switch
        {
            PeriodType.Daily => lastReturnPeriod.PeriodStart.AddDays(1),
            PeriodType.Monthly => lastReturnPeriod.PeriodStart.AddMonths(1),
            PeriodType.Yearly => lastReturnPeriod.PeriodStart.AddYears(1),
            _ => throw new InvalidOperationException()
        })
        - FirstNegativePeriodStart;
}
using Data.Returns;

namespace Data.BackTest
{
    public readonly struct BackTestRebalanceEvent
    {
        public string Ticker { get; init; }

        public DateTime PrecedingCompletedPeriodStart { get; init; }

        public PeriodType PrecedingCompletedPeriodType { get; init; }

        public decimal BalanceBeforeRebalance { get; init; }

        public decimal BalanceAfterRebalance { get; init; }

        public decimal PercentageChange => (BalanceAfterRebalance / BalanceBeforeRebalance - 1) * 100;

    }
}

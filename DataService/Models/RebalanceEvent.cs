using Data.Models;

namespace DataService.Models
{
    public readonly struct RebalanceEvent
    {
        public string Ticker { get; init; }

        public DateTime PrecedingCompletedPeriodStart { get; init; }

        public ReturnPeriod PrecedingCompletedPeriodType { get; init; }

        public decimal BalanceBeforeRebalance { get; init; }

        public decimal BalanceAfterRebalance { get; init; }

        public decimal PercentageChange { get { return ((BalanceAfterRebalance / BalanceBeforeRebalance) - 1) * 100; } }

    }
}

using Data.Models;

namespace DataService.Models
{
    public readonly struct NominalPeriodReturn(string ticker, decimal startingBalance, PeriodReturn periodReturn)
    {
        public string Ticker { get; init; } = ticker ?? periodReturn.SourceTicker;

        public ReturnPeriod ReturnPeriod { get; init; } = periodReturn.ReturnPeriod;

        public DateTime PeriodStart { get; init; } = periodReturn.PeriodStart;

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public decimal ReturnPercentage { get; init; } = periodReturn.ReturnPercentage;

        public decimal StartingBalance { get; init; } = startingBalance;

        public decimal EndingBalance { get { return StartingBalance + BalanceIncrease; } }

        public decimal BalanceIncrease { get { return StartingBalance * (this.ReturnPercentage / 100m); } }
    }

    public struct Rebalance
    {
        public string Ticker { get; init; }
        public DateTime PrecedingCompletedPeriodStart { get; init; }

        public ReturnPeriod PrecedingCompletedPeriodType { get; init; }

        public decimal BalanceBeforeRebalance { get; init; }

        public decimal BalanceAfterRebalance { get; init; }

        public decimal PercentageChange { get { return ((BalanceAfterRebalance / BalanceBeforeRebalance) - 1) * 100; } }

    }
}
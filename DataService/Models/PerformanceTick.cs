using Data.Models;

namespace DataService.Models
{

    public readonly struct PerformanceTick
    {
        public decimal StartingBalance { get; init; }

        public decimal EndingBalance { get { return StartingBalance + BalanceIncrease; } }

        public decimal BalanceIncrease { get; init; }

        public DateTime PeriodStart { get; init; }

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public decimal ReturnPercentage { get { return (EndingBalance / StartingBalance - 1) * 100; } }

        public ReturnPeriod ReturnPeriod { get; init; }

    }
}

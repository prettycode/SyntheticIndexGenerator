using Data.Models;

namespace DataService.Models
{
    public readonly struct PerformanceTick
    {
        public PeriodReturn Period { get; init; }

        public decimal StartingBalance { get; init; }

        public decimal EndingBalance { get { return StartingBalance + BalanceIncrease; } }

        public decimal BalanceIncrease { get { return StartingBalance * (Period.ReturnPercentage / 100m); } }
    }
}

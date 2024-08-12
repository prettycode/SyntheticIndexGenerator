namespace Data.Models
{
    public readonly struct BackTestPeriodReturn(string ticker, decimal startingBalance, PeriodReturn periodReturn)
    {
        public string Ticker { get; init; } = ticker ?? periodReturn.Ticker;

        public PeriodType PeriodType { get; init; } = periodReturn.PeriodType;

        public DateTime PeriodStart { get; init; } = periodReturn.PeriodStart;

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public decimal ReturnPercentage { get; init; } = periodReturn.ReturnPercentage;

        public decimal StartingBalance { get; init; } = startingBalance;

        public decimal EndingBalance => StartingBalance + BalanceIncrease;

        public decimal BalanceIncrease => StartingBalance * (ReturnPercentage / 100m);
    }
}
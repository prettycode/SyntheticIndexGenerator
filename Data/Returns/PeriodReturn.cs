namespace Data.Returns
{
    public readonly struct PeriodReturn
    {
        public string Ticker { get; init; }

        public DateTime PeriodStart { get; init; }

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public decimal ReturnPercentage { get; init; }

        public PeriodType PeriodType { get; init; }
    }
}
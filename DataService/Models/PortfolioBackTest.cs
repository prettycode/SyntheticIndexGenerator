namespace DataService.Models
{
    public struct PortfolioBackTest
    {
        public required NominalPeriodReturn[] AggregatePerformance { get; set; }

        public required Dictionary<string, NominalPeriodReturn[]> DecomposedPerformanceByTicker { get; set; }

        /// <summary>
        /// List of of the periods, after completing, that triggered rebalances.
        /// </summary>
        public required DateTime[] CompletedPeriodsBeforeRebalances { get; set; }

        public required RebalanceStrategy RebalanceStrategy { get; set; }

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public required decimal? RebalanceThreshold { get; set; }
    }
}

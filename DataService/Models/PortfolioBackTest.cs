namespace DataService.Models
{

    public class PortfolioBackTest
    {
        public NominalPeriodReturn[] AggregatePerformance { get; set; }

        public Dictionary<string, NominalPeriodReturn[]> DecomposedPerformanceByTicker { get; set; }

        /// <summary>
        /// List of of the periods, after completing, that triggered rebalances.
        /// </summary>
        public DateTime[] CompletedPeriodsBeforeRebalances { get; set; }

        public RebalanceStrategy RebalanceStrategy { get; set; }

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public decimal? RebalanceThreshold { get; set; }

    }
}

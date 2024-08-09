namespace Data.Models
{
    public struct BackTest
    {
        public required BackTestPeriodReturn[] AggregatePerformance { get; set; }

        public required Dictionary<string, BackTestPeriodReturn[]> DecomposedPerformanceByTicker { get; set; }

        /// <summary>
        /// List of of the periods, after completing, that triggered rebalances.
        /// </summary>
        public required Dictionary<string, BackTestRebalanceEvent[]> RebalancesByTicker { get; set; }

        public required BackTestRebalanceStrategy RebalanceStrategy { get; set; }

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public required decimal? RebalanceThreshold { get; set; }

        public double Cagr
        {
            get
            {
                var firstTick = AggregatePerformance[0];
                var lastTick = AggregatePerformance[^1];

                var lastTickStart = lastTick.PeriodStart;
                var lastTickPeriodType = lastTick.PeriodType;

                var firstPeriodStartDate = firstTick.PeriodStart;
                var lastPeriodStartDate = lastTickPeriodType switch
                {
                    PeriodType.Daily => lastTickStart.AddDays(1),
                    PeriodType.Monthly => lastTickStart.AddMonths(1),
                    PeriodType.Yearly => lastTickStart.AddYears(1),
                    _ => throw new InvalidOperationException()
                };

                double years = (lastPeriodStartDate - firstPeriodStartDate).TotalDays / 365.25;

                return Math.Pow(Convert.ToDouble(lastTick.EndingBalance) / Convert.ToDouble(firstTick.StartingBalance), 1 / years) - 1;
            }
        }

        public double YearsBeforeDoubling => Math.Log(2) / Math.Log(1 + Cagr);
    }
}
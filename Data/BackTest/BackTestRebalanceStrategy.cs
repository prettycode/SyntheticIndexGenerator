namespace Data.BackTest
{
    public enum BackTestRebalanceStrategy
    {
        /// <summary>
        /// Do not perform any rebalance strategy.
        /// </summary>
        None,

        /// <summary>
        /// Reset constituent allocations to target allocations every day.
        /// </summary>
        Daily,

        /// <summary>
        /// Reset constituent allocations to target allocations on the same day of the week as the start date, every week.
        /// </summary>
        Weekly,

        /// <summary>
        /// Reset constituent allocations to target allocations on the same day of the month as the start date, every month.
        /// </summary>
        Monthly,

        /// <summary>
        /// Reset constituent allocations to target allocations on the same day of the month as the start date, every three months.
        /// </summary>
        Quarterly,

        /// <summary>
        /// Reset constituent allocations to target allocations on the same day of the month as the start date, every six months.
        /// </summary>
        SemiAnnually,

        /// <summary>
        /// Reset constituent allocations to target allocations on the same day of the month as the start date, every year.
        /// </summary>
        Annually,

        BandsRelative,

        BandsAbsolute
    }
}

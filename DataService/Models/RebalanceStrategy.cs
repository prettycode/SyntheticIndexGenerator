namespace DataService.Models
{
    public enum RebalanceStrategy
    {
        None,
        Annually,
        SemiAnnually,
        Quarterly,
        /// <summary>
        /// Reset allocations to target allocation on the same date of the month as the start date.
        /// </summary>
        Monthly,
        /// <summary>
        /// Reset allocations to target allocation on the same day of the week as the start date.
        /// </summary>
        Weekly,
        Daily,
        BandsRelative,
        BandsAbsolute
    }
}

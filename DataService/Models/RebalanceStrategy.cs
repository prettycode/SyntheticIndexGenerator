namespace DataService.Models
{
    public enum RebalanceStrategy
    {
        None,
        Annually,
        SemiAnnually,
        Quarterly,
        Monthly,
        Weekly,
        Daily,
        BandsRelative,
        BandsAbsolute
    }
}

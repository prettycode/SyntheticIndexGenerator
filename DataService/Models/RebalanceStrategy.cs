namespace DataService.Models
{
    public enum RebalanceStrategy
    {
        None,
        Annually,
        SemiAnnually,
        Quarterly,
        Monthly,
        Daily,
        BandsRelative,
        BandsAbsolute
    }
}

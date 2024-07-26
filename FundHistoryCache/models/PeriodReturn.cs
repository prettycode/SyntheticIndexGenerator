public class PeriodReturn
{
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Scale is 0 - 100, not 0 - 1.
    /// </summary>
    public decimal ReturnPercentage { get; set; }

    public PeriodReturn(DateTime PeriodStart, decimal ReturnPercentage)
    {
        this.PeriodStart = PeriodStart;
        this.ReturnPercentage = ReturnPercentage;
    }

    [Obsolete($"Use '{nameof(PeriodReturn.PeriodStart)}' instead.", false)]
    public DateTime Key { get { return PeriodStart; } }


    [Obsolete($"Use '{nameof(PeriodReturn.ReturnPercentage)}' instead.", false)]
    public decimal Value { get { return ReturnPercentage; } }
}
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
}
public class PeriodReturn(DateTime periodStart, decimal returnPercentage, string sourceTicker, ReturnPeriod periodType)
{
    public DateTime PeriodStart { get; set; } = periodStart;

    /// <summary>
    /// Scale is 0 - 100, not 0 - 1.
    /// </summary>
    public decimal ReturnPercentage { get; set; } = returnPercentage;
    public string SourceTicker { get; set; } = sourceTicker;
    public ReturnPeriod ReturnPeriod { get; set; } = periodType;

    public string ToCsvLine()
    {
        return $"{this.PeriodStart:yyyy-MM-dd},{this.ReturnPercentage},{this.SourceTicker},{this.ReturnPeriod}";
    }

    public static PeriodReturn ParseCsvLine(string csvLine)
    {
        var cells = csvLine.Split(',');

        return new PeriodReturn(
            DateTime.Parse(cells[0]),
            decimal.Parse(cells[1]),
            cells[2],
            Enum.Parse<ReturnPeriod>(cells[3])
        );
    }
}
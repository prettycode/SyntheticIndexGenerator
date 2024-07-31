namespace FundHistoryCache.Models
{
    public readonly struct PeriodReturn
    {
        public DateTime PeriodStart { get; init; }

        /// <summary>
        /// Scale is 0 - 100, not 0 - 1.
        /// </summary>
        public decimal ReturnPercentage { get; init; }

        public string? SourceTicker { get; init; }

        public ReturnPeriod ReturnPeriod { get; init; }

        public string ToCsvLine()
        {
            return $"{PeriodStart:yyyy-MM-dd},{ReturnPercentage},{SourceTicker},{ReturnPeriod}";
        }

        public static PeriodReturn ParseCsvLine(string csvLine)
        {
            var cells = csvLine.Split(',');

            return new()
            {
                PeriodStart = DateTime.Parse(cells[0]),
                ReturnPercentage = decimal.Parse(cells[1]),
                SourceTicker = cells[2],
                ReturnPeriod = Enum.Parse<ReturnPeriod>(cells[3])
            };
        }
    }
}
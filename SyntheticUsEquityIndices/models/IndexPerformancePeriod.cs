private struct IndexPeriodPerformance
{
    public required IndexId IndexId { get; set; }
    public required DateOnly PeriodStartDate { get; set; }
    public required decimal PeriodReturnPercent { get; set; }
}

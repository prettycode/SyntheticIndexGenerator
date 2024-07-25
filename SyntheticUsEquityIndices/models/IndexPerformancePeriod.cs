public readonly record struct IndexPeriodPerformance
{
    public required IndexId IndexId { get; init; }
    public required DateOnly PeriodStartDate { get; init; }
    public required decimal PeriodReturnPercent { get; init; }
}

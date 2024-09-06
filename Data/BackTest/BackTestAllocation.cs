namespace Data.BackTest;

public readonly struct BackTestAllocation
{
    public required string Ticker { get; init; }

    /// <summary>
    /// Scale is 0 - 100, not 0 - 1.
    /// </summary>
    public required decimal Percentage { get; init; }
}

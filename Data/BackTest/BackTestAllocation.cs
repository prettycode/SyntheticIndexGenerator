namespace Data.BackTest;

public readonly struct BackTestAllocation
{
    public string Ticker { get; init; }

    /// <summary>
    /// Scale is 0 - 100, not 0 - 1.
    /// </summary>
    public decimal Percentage { get; init; }
}

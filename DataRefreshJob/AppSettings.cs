namespace DataRefreshJob;

public class AppSettings
{
    public required string QuoteRepositoryDataPath { get; set; }
    public required string ReturnRepositoryDataPath { get; set; }
    public required string SyntheticReturnsFilePath { get; set; }
}

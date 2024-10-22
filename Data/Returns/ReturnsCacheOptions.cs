using Microsoft.Extensions.Options;
using TableFileCache;

namespace Data.Returns;

public class ReturnsCacheOptions : IOptions<ReturnsCacheOptions>
{
    public required string SyntheticUsMarketReturnsFilePath { get; init; }

    public required string SyntheticAlternativesFilePathPattern { get; init; }

    public required TableFileCacheOptions TableCacheOptions { get; init; }

    ReturnsCacheOptions IOptions<ReturnsCacheOptions>.Value => this;
}

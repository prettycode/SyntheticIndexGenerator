using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache.DaylongCache.GenericMemoryCache;

public class GenericMemoryCacheOptions : IOptions<GenericMemoryCacheOptions>
{
    public MemoryCacheOptions? MemoryCacheOptions { get; init; }

    public MemoryCacheEntryOptions? MemoryCacheEntryOptions { get; init; }

    GenericMemoryCacheOptions IOptions<GenericMemoryCacheOptions>.Value
    {
        get { return this; }
    }
}

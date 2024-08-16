using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.TableFileCache.DaylongCache;
using Microsoft.Extensions.Options;

namespace Data.TableFileCache;

public class TableCacheOptions : IOptions<TableCacheOptions>
{
    public required string CacheRootPath { get; init; } = "./";

    public string? CacheNamespace { get; init; }

    public required DaylongCacheOptions DaylongCacheOptions { get; init; }

    TableCacheOptions IOptions<TableCacheOptions>.Value
    {
        get { return this; }
    }
}

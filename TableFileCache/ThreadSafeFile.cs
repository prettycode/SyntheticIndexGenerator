using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableFileCache;
public static class ThreadSafeFile
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new();

    public static async Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
    {
        var fileLock = fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync();

        try
        {
            return await File.ReadAllLinesAsync(path, cancellationToken);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public static async Task AppendAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        CancellationToken cancellationToken = default)
    {
        var fileLock = fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync();

        try
        {
            await File.AppendAllLinesAsync(path, contents, cancellationToken);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public static async Task WriteAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        CancellationToken cancellationToken = default)
    {
        var fileLock = fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync();

        try
        {
            await File.WriteAllLinesAsync(path, contents, cancellationToken);
        }
        finally
        {
            fileLock.Release();
        }
    }
}

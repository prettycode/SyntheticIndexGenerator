using System.Collections.Concurrent;

namespace TableFileCache;

public static class ThreadSafeFile
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new();

    private static async Task ExecuteWithFileLockAsync(
        string path,
        Func<string, CancellationToken, Task>
        fileOperation,
        CancellationToken cancellationToken = default)
    {
        var fileLock = fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync(cancellationToken);

        try
        {
            await fileOperation(path, cancellationToken);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private static async Task<T> ExecuteWithFileLockAsync<T>(
        string path,
        Func<string, CancellationToken, Task<T>> fileOperation,
        CancellationToken cancellationToken = default)
    {
        var fileLock = fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync(cancellationToken);

        try
        {
            return await fileOperation(path, cancellationToken);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public static Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
    {
        return ExecuteWithFileLockAsync(path, File.ReadAllLinesAsync, cancellationToken);
    }

    public static Task AppendAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithFileLockAsync(path, (p, ct)
            => File.AppendAllLinesAsync(p, contents, ct), cancellationToken);
    }

    public static Task WriteAllLinesAsync(
        string path,
        IEnumerable<string> contents,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithFileLockAsync(path, (p, ct)
            => File.WriteAllLinesAsync(p, contents, ct), cancellationToken);
    }
}
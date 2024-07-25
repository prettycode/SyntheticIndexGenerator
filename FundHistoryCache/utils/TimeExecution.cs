
using System.Diagnostics;

public static class TimerUtility
{
    public static async Task<(T Result, TimeSpan Elapsed)> TimeExecution<T>(string operationName, Func<Task<T>> operation)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        T result = await operation();

        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;

        Console.WriteLine($"\"{operationName}\" execution time: {elapsed.TotalMilliseconds} ms");

        return (result, elapsed);
    }

    public static async Task<TimeSpan> TimeExecution(string operationName, Func<Task> operation)
    {
        var (_, elapsed) = await TimeExecution(operationName, async () =>
        {
            await operation();
            return 0;
        });

        return elapsed;
    }
}

using System.Diagnostics;

namespace FundHistoryCache.Utils
{
    public static class Timer
    {
        private static async Task<(T Result, TimeSpan Elapsed)> ExecInternal<T>(string operationName, Func<Task<T>> operation)
        {
            Stopwatch stopwatch = new();

            stopwatch.Start();
            T result = await operation();
            stopwatch.Stop();

            TimeSpan elapsed = stopwatch.Elapsed;

            Console.WriteLine($"\"{operationName}\" execution time: {elapsed.TotalMilliseconds} ms");

            return (result, elapsed);
        }

        public static Task<(T Result, TimeSpan Elapsed)> Exec<T>(string operationName, Func<Task<T>> operation)
            => ExecInternal(operationName, operation);

        public static async Task<TimeSpan> Exec(string operationName, Func<Task> operation)
        {
            var (_, elapsed) = await ExecInternal(operationName, async () =>
            {
                await operation();
                return 0;
            });
            return elapsed;
        }

        public static Task<TimeSpan> Exec(string operationName, Task task)
            => Exec(operationName, () => task);
    }
}
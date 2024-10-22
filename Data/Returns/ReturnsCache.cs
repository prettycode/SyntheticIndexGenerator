﻿using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableFileCache;

namespace Data.Returns;

internal class ReturnsCache : IReturnsCache
{
    private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    private static bool havePutSyntheticsInRepository = false;

    private readonly string syntheticReturnsFilePath;

    private readonly TableFileCache<string, PeriodReturn> dailyCache;

    private readonly TableFileCache<string, PeriodReturn> monthlyCache;

    private readonly TableFileCache<string, PeriodReturn> yearlyCache;

    private readonly ILogger<ReturnsCache> logger;

    public static async Task<ReturnsCache> Create(
        IOptions<ReturnsCacheOptions> options,
        ILogger<ReturnsCache> logger)
    {
        var instance = new ReturnsCache(options, logger);

        await semaphoreSlim.WaitAsync();

        try
        {
            logger.LogInformation(
                "Synthetic returns have been put in repository: {syntheticsLoaded}",
                havePutSyntheticsInRepository);

            if (!havePutSyntheticsInRepository)
            {
                await instance.PutSyntheticsInRepository();

                havePutSyntheticsInRepository = true;
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return instance;
    }

    private ReturnsCache(IOptions<ReturnsCacheOptions> returnRepositoryOptions, ILogger<ReturnsCache> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var options = returnRepositoryOptions.Value;
        var tableCacheOptions = options.TableCacheOptions;

        dailyCache = new(tableCacheOptions, $"{PeriodType.Daily}");
        monthlyCache = new(tableCacheOptions, $"{PeriodType.Monthly}");
        yearlyCache = new(tableCacheOptions, $"{PeriodType.Yearly}");

        syntheticReturnsFilePath = options.SyntheticReturnsFilePath;

        if (!File.Exists(syntheticReturnsFilePath))
        {
            throw new ArgumentException($"{syntheticReturnsFilePath} file does not exist.", nameof(returnRepositoryOptions));
        }

        this.logger = logger;
    }

    public Task<IEnumerable<PeriodReturn>?> TryGetValue(string ticker, PeriodType periodType) => GetPeriodTypeCache(periodType).TryGetValue(ticker);

    public async Task<List<PeriodReturn>> Get(
        string ticker,
        PeriodType periodType,
        DateTime? firstPeriod = null,
        DateTime? lastPeriod = null)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(periodType);

        var cache = GetPeriodTypeCache(periodType);
        var cacheRecords = await cache.Get(ticker);

        return cacheRecords
            .Where(pair =>
                (firstPeriod == null || pair.PeriodStart >= firstPeriod) &&
                (lastPeriod == null || pair.PeriodStart <= lastPeriod))
            .ToList();
    }

    private TableFileCache<string, PeriodReturn> GetPeriodTypeCache(PeriodType periodType) => periodType switch
    {
        PeriodType.Daily => dailyCache,
        PeriodType.Monthly => monthlyCache,
        PeriodType.Yearly => yearlyCache,
        _ => throw new NotImplementedException()
    };

    public async Task<List<PeriodReturn>> Put(string ticker, IEnumerable<PeriodReturn> returns, PeriodType periodType)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(returns);
        ArgumentNullException.ThrowIfNull(periodType);

        if (!returns.Any())
        {
            throw new ArgumentException("Cannot be empty.", nameof(returns));
        }

        var cache = GetPeriodTypeCache(periodType);

        logger.LogInformation("{ticker}: Writing returns for period type {periodType}.", ticker, periodType);

        return [.. await cache.Put(ticker, returns)];
    }

    private async Task PutSyntheticsInRepository()
    {
        var allPutTasks = new List<Task>(CreateSyntheticAlternativesReturns());

        var monthlyReturnsTask = CreateSyntheticUsMarketMonthlyReturns();
        var yearlyReturnsTask = CreateSyntheticUsMarketYearlyReturns();

        logger.LogInformation("Getting synthetic monthly and yearly returns...");

        await Task.WhenAll(monthlyReturnsTask, yearlyReturnsTask);

        logger.LogInformation("Got synthetic monthly and yearly returns. Adding to returns repository...");

        foreach (var (ticker, returns) in monthlyReturnsTask.Result)
        {
            allPutTasks.Add(Put(ticker, returns, PeriodType.Monthly));
        }

        foreach (var (ticker, returns) in yearlyReturnsTask.Result)
        {
            allPutTasks.Add(Put(ticker, returns, PeriodType.Yearly));
        }

        allPutTasks.AddRange(CreateFakeSyntheticReturnsPutTasks());

        await Task.WhenAll(allPutTasks);

        logger.LogInformation("Synthetic monthly and yearly returns added to repository.");
    }

    private IEnumerable<Task> CreateFakeSyntheticReturnsPutTasks()
    {
        IEnumerable<PeriodReturn> GetReturns(string ticker, IEnumerable<DateTime> dates, decimal returnPercentage, PeriodType periodType)
            => dates.Select(date => new PeriodReturn
            {
                Ticker = ticker,
                PeriodStart = date,
                ReturnPercentage = returnPercentage,
                PeriodType = periodType
            });

        var dailyDates = new[]
        {
            new DateTime(2023, 1, 3),
            new DateTime(2023, 1, 4),
            new DateTime(2023, 1, 5),
            new DateTime(2023, 1, 6),
            new DateTime(2023, 1, 9),
            new DateTime(2023, 1, 10),
            new DateTime(2023, 1, 11),
            new DateTime(2023, 1, 12),
            new DateTime(2023, 1, 13),
            new DateTime(2023, 1, 17),
            new DateTime(2023, 1, 18),
            new DateTime(2023, 1, 19),
            new DateTime(2023, 1, 20),
            new DateTime(2023, 1, 23),
            new DateTime(2023, 1, 24),
            new DateTime(2023, 1, 25),
            new DateTime(2023, 1, 26),
            new DateTime(2023, 1, 27),
            new DateTime(2023, 1, 30),
            new DateTime(2023, 1, 31),
            new DateTime(2023, 2, 1),
            new DateTime(2023, 2, 2),
            new DateTime(2023, 2, 3),
            new DateTime(2023, 2, 6),
            new DateTime(2023, 2, 7)
        };

        var monthlyDates = Enumerable
            .Range(1, 12)
            .Select(month => new DateTime(2023, month, 1));

        var tickerData = new[]
        {
            (Ticker: "#1X", ReturnPercentage: 0m),
            (Ticker: "#2X", ReturnPercentage: 100m),
            (Ticker: "#3X", ReturnPercentage: 200m)
        };

        return tickerData.SelectMany(td =>
        {
            var dailyReturns = GetReturns(td.Ticker, dailyDates, td.ReturnPercentage, PeriodType.Daily);
            var monthlyReturns = GetReturns(td.Ticker, monthlyDates, td.ReturnPercentage, PeriodType.Monthly);

            return new[]
            {
                Put(td.Ticker, dailyReturns, PeriodType.Daily),
                Put(td.Ticker, monthlyReturns, PeriodType.Monthly)
            };
        });
    }

    private IEnumerable<Task> CreateSyntheticAlternativesReturns()
    {
        const int headerLinesCount = 1;
        const int dateColumnIndex = 0;
        const int balanceColumnIndex = 1;

        var syntheticTickers = new HashSet<string>() { "TBILL", "GOLDX", "KMLMX", "DBMFX" };

        async Task ProcessTickerAsync(string ticker)
        {
            var tickerReturns = new List<PeriodReturn>();
            // TODO hacky, fix
            var filePath = Path.Combine(Path.GetDirectoryName(syntheticReturnsFilePath), $"{ticker}.csv");
            var fileLines = await ThreadSafeFile.ReadAllLinesAsync(filePath);
            var fileLinesSansHeader = fileLines.Skip(headerLinesCount).ToList();
            decimal? previousEndingBalance = null;

            for (var i = 0; i < fileLinesSansHeader.Count; i++)
            {
                var line = fileLinesSansHeader[i];
                var cells = line.Replace("\"", string.Empty).Split(',');
                var endingBalanceOnDate = decimal.Parse(cells[balanceColumnIndex]);
                var dateOfEndingBalance = DateTime.ParseExact(cells[dateColumnIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture);

                if (previousEndingBalance == null)
                {
                    previousEndingBalance = endingBalanceOnDate;
                    continue;
                }

                tickerReturns.Add(new PeriodReturn()
                {
                    PeriodStart = dateOfEndingBalance,
                    PeriodType = PeriodType.Daily,
                    Ticker = ticker,
                    ReturnPercentage = 100 * ((endingBalanceOnDate - previousEndingBalance.Value) / previousEndingBalance.Value)
                });

                previousEndingBalance = endingBalanceOnDate;
            }

            await Put(ticker, tickerReturns, PeriodType.Daily);
        }

        return syntheticTickers.Select(ticker => ProcessTickerAsync(ticker));
    }


    private async Task<Dictionary<string, List<PeriodReturn>>> CreateSyntheticUsMarketMonthlyReturns()
    {
        var columnIndexToCategory = new Dictionary<int, string>
        {
            [1] = "$USTSM",
            [3] = "$USLCB",
            [4] = "$USLCV",
            [5] = "$USLCG",
            [6] = "$USMCB",
            [7] = "$USMCV",
            [8] = "$USMCG",
            [9] = "$USSCB",
            [10] = "$USSCV",
            [11] = "$USSCG"
        };

        const int headerLinesCount = 1;
        const int dateColumnIndex = 0;

        var returns = new Dictionary<string, List<PeriodReturn>>();
        var fileLines = await File.ReadAllLinesAsync(syntheticReturnsFilePath);
        var fileLinesSansHeader = fileLines.Skip(headerLinesCount);

        foreach (var line in fileLinesSansHeader)
        {
            var cells = line.Split(',');
            var date = DateTime.ParseExact(cells[dateColumnIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture);

            foreach (var (cellIndex, ticker) in columnIndexToCategory)
            {
                var cellValue = decimal.Parse(cells[cellIndex], NumberStyles.Any, CultureInfo.InvariantCulture);

                if (!returns.TryGetValue(ticker, out var tickerReturns))
                {
                    tickerReturns = returns[ticker] = [];
                }

                // decimal.ToString("G29") will trim trailing 0s
                tickerReturns.Add(new PeriodReturn()
                {
                    PeriodStart = date,
                    ReturnPercentage = decimal.Parse($"{cellValue:G29}"),
                    Ticker = ticker,
                    PeriodType = PeriodType.Monthly
                });
            }
        }

        return returns;
    }

    private async Task<Dictionary<string, List<PeriodReturn>>> CreateSyntheticUsMarketYearlyReturns()
    {
        static List<PeriodReturn> CalculateYearlyFromMonthly(string ticker, List<PeriodReturn> monthlyReturns)
        {
            var result = new List<PeriodReturn>();

            var yearStarts = monthlyReturns
                .GroupBy(r => r.PeriodStart.Year)
                .Select(g => g.OrderBy(r => r.PeriodStart).First())
                .OrderBy(r => r.PeriodStart)
                .ToArray();

            var i = yearStarts[0].PeriodStart.Month == 1 ? 0 : 1;

            for (; i < yearStarts.Length; i++)
            {
                var currentYear = yearStarts[i].PeriodStart.Year;
                var currentYearMonthlyReturns = monthlyReturns.Where(r => r.PeriodStart.Year == currentYear);
                var currentYearAggregateReturn = currentYearMonthlyReturns.Aggregate(1.0m, (acc, item) => acc * (1 + item.ReturnPercentage / 100)) - 1;
                var periodReturn = new PeriodReturn()
                {
                    PeriodStart = new DateTime(currentYear, 1, 1),
                    ReturnPercentage = currentYearAggregateReturn * 100,
                    Ticker = ticker,
                    PeriodType = PeriodType.Yearly
                };

                result.Add(periodReturn);
            }

            return result;
        }

        var monthlyReturns = await CreateSyntheticUsMarketMonthlyReturns();
        var yearlyReturns = monthlyReturns.ToDictionary(pair => pair.Key, pair => CalculateYearlyFromMonthly(pair.Key, pair.Value));

        return yearlyReturns;
    }
}
﻿using System.Globalization;

public class ReturnsRepository
{
    private readonly string cachePath;

    private readonly string syntheticReturnsFilePath;

    public ReturnsRepository(string cachePath, string syntheticReturnsFilePath)
    {
        ArgumentNullException.ThrowIfNull(cachePath);

        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }

        this.cachePath = cachePath;
        this.syntheticReturnsFilePath = syntheticReturnsFilePath;
    }

    public bool Has(string ticker, ReturnPeriod period, out string cacheFilePath)
    {
        cacheFilePath = this.GetCsvFilePath(ticker, period);

        return File.Exists(cacheFilePath);
    }

    public Task<List<PeriodReturn>?> Get(string ticker, ReturnPeriod period)
    {
        return this.Get(ticker, period, DateTime.MinValue, DateTime.MaxValue);
    }

    public async Task<List<PeriodReturn>?> Get(string ticker, ReturnPeriod period, DateTime start, DateTime end)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(period);

        if (!this.Has(ticker, period, out string csvFilePath))
        {
            return null;
        }

        var csvLines = await File.ReadAllLinesAsync(csvFilePath);
        var allReturns = csvLines.Select(line => PeriodReturn.ParseCsvLine(line));

        return allReturns.Where(pair => pair.PeriodStart >= start && pair.PeriodStart <= end).ToList();
    }

    public Task<List<PeriodReturn>?> GetMostGranular(string ticker, out ReturnPeriod period)
    {
        ReturnPeriod[] periodsToCheck =
        [
            ReturnPeriod.Daily,
            ReturnPeriod.Monthly,
            ReturnPeriod.Yearly
        ];

        foreach (var checkPeriod in periodsToCheck)
        {
            if (this.Has(ticker, checkPeriod, out string csvFilePath))
            {
                period = checkPeriod;
                return this.Get(ticker, checkPeriod);
            }
        }

        throw new InvalidOperationException($"Returns for '{ticker}' not for any period.");
    }

    public Task Put(string ticker, List<PeriodReturn> returns, ReturnPeriod period)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(returns);
        ArgumentNullException.ThrowIfNull(period);

        var csvFilePath = this.GetCsvFilePath(ticker, period);
        var csvDirPath = Path.GetDirectoryName(csvFilePath);

        if (!Directory.Exists(csvDirPath))
        {
            Directory.CreateDirectory(csvDirPath!);
        }

        var csvFileLines = returns.Select(r => r.ToCsvLine());

        return File.WriteAllLinesAsync(csvFilePath, csvFileLines);
    }

    public async Task<Dictionary<string, List<PeriodReturn>>> GetSyntheticMonthlyReturns()
    {
        var columnIndexToCategory = new Dictionary<int, string>
        {
            [1] = "$TSM",
            [3] = "$LCB",
            [4] = "$LCV",
            [5] = "$LCG",
            [6] = "$MCB",
            [7] = "$MCV",
            [8] = "$MCG",
            [9] = "$SCB",
            [10] = "$SCV",
            [11] = "$SCG"
        };

        const int headerLinesCount = 1;
        const int dateColumnIndex = 0;

        var returns = new Dictionary<string, List<PeriodReturn>>();
        var fileLines = await File.ReadAllLinesAsync(this.syntheticReturnsFilePath);
        var fileLinesSansHeader = fileLines.Skip(headerLinesCount);

        foreach (var line in fileLinesSansHeader)
        {
            var cells = line.Split(',');
            var date = DateTime.Parse(cells[dateColumnIndex]);

            foreach (var (currentCell, ticker) in columnIndexToCategory)
            {
                if (decimal.TryParse(cells[currentCell], NumberStyles.Any, CultureInfo.InvariantCulture, out var cellValue))
                {
                    if (!returns.TryGetValue(ticker, out var value))
                    {
                        value = returns[ticker] = [];
                    }

                    value.Add(new PeriodReturn(date, decimal.Parse($"{cellValue:G29}"), ticker, ReturnPeriod.Monthly));
                }
            }
        }

        return returns;
    }

    private string GetCsvFilePath(string ticker, ReturnPeriod period)
    {
        return Path.Combine(this.cachePath, $"./{period.ToString().ToLowerInvariant()}/{ticker}.csv");
    }
}
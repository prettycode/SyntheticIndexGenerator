using System.Globalization;

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

    public Task<List<KeyValuePair<DateTime, decimal>>?> TryGet(string ticker, ReturnPeriod period)
    {
        return this.TryGet(ticker, period, DateTime.MinValue, DateTime.MaxValue);
    }

    public async Task<List<KeyValuePair<DateTime, decimal>>?> TryGet(string ticker, ReturnPeriod period, DateTime start, DateTime end)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(period);

        var csvDirPath = Path.Combine(this.cachePath, $"./{period.ToString().ToLowerInvariant()}");
        var csvFilePath = Path.Combine(csvDirPath, $"./{ticker}.csv");

        if (!File.Exists(csvFilePath))
        {
            return null!;
        }

        var csvLines = await File.ReadAllLinesAsync(csvFilePath);
        var csvLinesSplit = csvLines.Select(line => line.Split(','));
        var allReturns = csvLinesSplit.Select(cells => new KeyValuePair<DateTime, decimal>(DateTime.Parse(cells[0]), decimal.Parse(cells[1])));

        return allReturns.Where(pair => pair.Key >= start && pair.Key <= end).ToList();
    }

    public Task Put(string ticker, List<KeyValuePair<DateTime, decimal>> returns, ReturnPeriod period)
    {
        ArgumentNullException.ThrowIfNull(ticker);
        ArgumentNullException.ThrowIfNull(returns);
        ArgumentNullException.ThrowIfNull(period);

        var csvDirPath = Path.Combine(this.cachePath, $"./{period.ToString().ToLowerInvariant()}");

        if (!Directory.Exists(csvDirPath))
        {
            Directory.CreateDirectory(csvDirPath);
        }

        var csvFilePath = Path.Combine(csvDirPath, $"./{ticker}.csv");
        var csvFileLines = returns.Select(r => $"{r.Key:yyyy-MM-dd},{r.Value}");

        return File.WriteAllLinesAsync(csvFilePath, csvFileLines);
    }

    public async Task<Dictionary<string, List<KeyValuePair<DateTime, decimal>>>> GetSyntheticMonthlyReturns()
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

        var returns = new Dictionary<string, List<KeyValuePair<DateTime, decimal>>>();
        var fileLines = await File.ReadAllLinesAsync(this.syntheticReturnsFilePath);
        var fileLinesSansHeader = fileLines.Skip(headerLinesCount);

        foreach (var line in fileLinesSansHeader)
        {
            var cells = line.Split(',');
            var date = DateTime.Parse(cells[dateColumnIndex]);

            foreach (var (currentCell, cellCategory) in columnIndexToCategory)
            {
                if (decimal.TryParse(cells[currentCell], NumberStyles.Any, CultureInfo.InvariantCulture, out var cellValue))
                {
                    if (!returns.TryGetValue(cellCategory, out var value))
                    {
                        value = returns[cellCategory] = [];
                    }

                    value.Add(new KeyValuePair<DateTime, decimal>(date, decimal.Parse($"{cellValue:G29}")));
                }
            }
        }

        return returns;
    }
}
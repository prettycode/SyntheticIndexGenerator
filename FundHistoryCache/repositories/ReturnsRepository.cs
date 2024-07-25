using System.Globalization;

public class ReturnsRepository
{
    public string CachePath { get; private set; }

    public string SyntheticReturnsFilePath { get; private set; }

    public ReturnsRepository(string cachePath, string syntheticReturnsFilePath)
    {
        ArgumentNullException.ThrowIfNull(cachePath);

        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }

        this.CachePath = cachePath;
        this.SyntheticReturnsFilePath = syntheticReturnsFilePath;
    }

    public Task Put(string ticker, List<KeyValuePair<DateTime, decimal>> returns, ReturnsController.TimePeriod period)
    {
        string csvFilePath = Path.Combine(this.CachePath, $"./{period.ToString().ToLowerInvariant()}/{ticker}.csv");
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
        var fileLines = await File.ReadAllLinesAsync(this.SyntheticReturnsFilePath);
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
using System.Globalization;

internal static class SyntheticReturnsController
{

    private struct SyntheticReturn
    {
        public DateOnly PeriodStartDate { get; set; }
        public decimal PeriodReturnPercent { get; set; }
    }

    public static async Task RefreshSyntheticReturns(string csvFilePath, string savePath)
    {
        var indexReturns = await SyntheticReturnsController.GetMonthlyReturns(csvFilePath);

        await SyntheticReturnsController.SaveReturns(indexReturns, savePath);
    }

    private static Task SaveReturns(Dictionary<string, List<SyntheticReturn>> history, string savePath)
    {
        return Task.WhenAll(history.Select(pair => {
            var tickerHistoryFilename = Path.Combine(savePath, $"{pair.Key}.csv");
            var lines = pair.Value.Select(r => $"{r.PeriodStartDate:yyyy-MM-dd},{r.PeriodReturnPercent:G29}");

            return File.WriteAllLinesAsync(tickerHistoryFilename, lines);
        }));
    }

    private static async Task<Dictionary<string, List<SyntheticReturn>>> GetMonthlyReturns(string csvFilename)
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

        var returns = new Dictionary<string, List<SyntheticReturn>>();
        var fileLines = await File.ReadAllLinesAsync(csvFilename);
        var fileLinesSansHeader = fileLines.Skip(headerLinesCount);

        foreach (var line in fileLinesSansHeader)
        {
            var cells = line.Split(',');
            var date = DateOnly.Parse(cells[dateColumnIndex]);

            foreach (var (currentCell, cellCategory) in columnIndexToCategory)
            {
                if (decimal.TryParse(cells[currentCell], NumberStyles.Any, CultureInfo.InvariantCulture, out var cellValue))
                {
                    if (!returns.TryGetValue(cellCategory, out var value))
                    {
                        value = returns[cellCategory] = [];
                    }

                    value.Add(new()
                    {
                        PeriodStartDate = date,
                        PeriodReturnPercent = cellValue
                    });
                }
            }
        }

        return returns;
    }
}
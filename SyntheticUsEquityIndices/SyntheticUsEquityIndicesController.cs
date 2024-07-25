using System.Globalization;

public static class SyntheticUsEquityIndicesController
{
    public static async Task SaveParsedReturnsToReturnsHistory(string csvFilePath, string savePath)
    {
        var indexReturns = await SyntheticUsEquityIndicesController.ParseReturns(csvFilePath);

        await SyntheticUsEquityIndicesController.SaveReturnsHistory(indexReturns, savePath);
    }

    public static async Task SaveReturnsHistory(Dictionary<IndexId, SortedDictionary<DateOnly, IndexPeriodPerformance>> multiIndexReturns, string savePath)
    {
        var indexToTicker = new Dictionary<IndexId, string>
        {
            [IndexId.TotalStockMarket] = "^USTSM",
            [IndexId.LargeCapBlend] = "^USLCB",
            [IndexId.LargeCapValue] = "^USLCV",
            [IndexId.LargeCapGrowth] = "^USLCG",
            [IndexId.MidCapBlend] = "^USMCB",
            [IndexId.MidCapValue] = "^USMCV",
            [IndexId.MidCapGrowth] = "^USMCG",
            [IndexId.SmallCapBlend] = "^USSCB",
            [IndexId.SmallCapValue] = "^USSCV",
            [IndexId.SmallCapGrowth] = "^USSCG"
        };

        foreach (var (index, returns) in multiIndexReturns)
        {
            var tickerHistoryFilename = Path.Combine(savePath, $"{indexToTicker[index]}.csv");
            var lines = returns.Select(r => $"{r.Key:yyyy-MM-dd},{r.Value.PeriodReturnPercent:G29}");

            await File.WriteAllLinesAsync(tickerHistoryFilename, lines);
        }
    }

    public static async Task<Dictionary<IndexId, SortedDictionary<DateOnly, IndexPeriodPerformance>>> ParseReturns(string csvFilename)
    {
        var columnIndexToCategory = new Dictionary<int, IndexId>
        {
            [1] = IndexId.TotalStockMarket,
            [3] = IndexId.LargeCapBlend,
            [4] = IndexId.LargeCapValue,
            [5] = IndexId.LargeCapGrowth,
            [6] = IndexId.MidCapBlend,
            [7] = IndexId.MidCapValue,
            [8] = IndexId.MidCapGrowth,
            [9] = IndexId.SmallCapBlend,
            [10] = IndexId.SmallCapValue,
            [11] = IndexId.SmallCapGrowth
        };

        const int headerLinesCount = 1;
        const int dateColumnIndex = 0;

        var returns = new Dictionary<IndexId, SortedDictionary<DateOnly, IndexPeriodPerformance>>();
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

                    value[date] = new IndexPeriodPerformance
                    {
                        PeriodStartDate = date,
                        IndexId = cellCategory,
                        PeriodReturnPercent = cellValue
                    };
                }
            }
        }

        return returns;
    }
}
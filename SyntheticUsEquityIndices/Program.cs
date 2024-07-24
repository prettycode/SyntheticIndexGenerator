using System.Globalization;

await Load(await Extract());

static async Task Load(Dictionary<IndexId, SortedDictionary<DateOnly, IndexPeriodPerformance>> multiIndexReturns, string pathPath = @"..\..\..\data\")
{
    var indexToTicker = new Dictionary<IndexId, string>
    {
        [IndexId.TotalStockMarket] = "^USTSM",
        [IndexId.LargeCapBlend] = "^USLCB",
        [IndexId.LargeCapValue] = "^USLCV",
        [IndexId.LargeCapGrowth] = "^USLCG",
        [IndexId.MidCapBlend] = "^USMDB",
        [IndexId.MidCapValue] = "^USMDV",
        [IndexId.MidCapGrowth] = "^USMDG",
        [IndexId.SmallCapBlend] = "^USSCB",
        [IndexId.SmallCapValue] = "^USSCV",
        [IndexId.SmallCapGrowth] = "^USSCG"
    };

    foreach (var (index, returns) in multiIndexReturns)
    {
        var tickerHistoryFilename = Path.Combine(pathPath, $"{indexToTicker[index]}.monthly.csv");
        var lines = returns.Select(r => $"{r.Key:yyyy-MM-dd},{r.Value.PeriodReturnPercent:G29}");

        await File.WriteAllLinesAsync(tickerHistoryFilename, lines);
    }
}

static async Task<Dictionary<IndexId, SortedDictionary<DateOnly, IndexPeriodPerformance>>> Extract(string csvFilename = @"..\..\..\source\Stock-Index-Data-20220923-Monthly.csv")
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

enum IndexId
{
    TotalStockMarket,
    LargeCapBlend,
    LargeCapGrowth,
    LargeCapValue,
    MidCapBlend,
    MidCapGrowth,
    MidCapValue,
    SmallCapBlend,
    SmallCapGrowth,
    SmallCapValue
}

readonly record struct IndexPeriodPerformance
{
    public required IndexId IndexId { get; init; }
    public required DateOnly PeriodStartDate { get; init; }
    public required decimal PeriodReturnPercent { get; init; }
}

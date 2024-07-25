﻿using System;
using System.Globalization;

public static class FundHistorySyntheticReturnsController
{    private enum IndexId
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

    private struct IndexPeriodPerformance
    {
        public required IndexId IndexId { get; set; }
        public required DateOnly PeriodStartDate { get; set; }
        public required decimal PeriodReturnPercent { get; set; }
    }

    public static async Task RefreshFundHistorySyntheticReturns(string csvFilePath, string savePath)
    {
        var indexReturns = await FundHistorySyntheticReturnsController.ParseReturns(csvFilePath);

        await FundHistorySyntheticReturnsController.SaveReturnsHistory(indexReturns, savePath);
    }

    private static async Task SaveReturnsHistory(Dictionary<IndexId, SortedDictionary<DateOnly, IndexPeriodPerformance>> multiIndexReturns, string savePath)
    {
        var indexToTicker = new Dictionary<IndexId, string>
        {
            [IndexId.TotalStockMarket] = "$TSM",
            [IndexId.LargeCapBlend] = "$LCB",
            [IndexId.LargeCapValue] = "$LCV",
            [IndexId.LargeCapGrowth] = "$LCG",
            [IndexId.MidCapBlend] = "$MCB",
            [IndexId.MidCapValue] = "$MCV",
            [IndexId.MidCapGrowth] = "$MCG",
            [IndexId.SmallCapBlend] = "$SCB",
            [IndexId.SmallCapValue] = "$SCV",
            [IndexId.SmallCapGrowth] = "$SCG"
        };

        foreach (var (index, returns) in multiIndexReturns)
        {
            var tickerHistoryFilename = Path.Combine(savePath, $"{indexToTicker[index]}.csv");
            var lines = returns.Select(r => $"{r.Key:yyyy-MM-dd},{r.Value.PeriodReturnPercent:G29}");

            await File.WriteAllLinesAsync(tickerHistoryFilename, lines);
        }
    }

    private static async Task<Dictionary<IndexId, SortedDictionary<DateOnly, IndexPeriodPerformance>>> ParseReturns(string csvFilename)
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
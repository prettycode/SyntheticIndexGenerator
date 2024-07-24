using System.Globalization;

await RefreshFundHistoryCache(new FundHistoryRepository());

static async Task RefreshFundHistoryCache(FundHistoryRepository cache)
{
    foreach (var ticker in GetFundTickers())
    {
        Console.WriteLine();

        var fundHistory = await cache.Get(ticker);

        if (fundHistory == null)
        {
            Console.WriteLine($"{ticker}: No history found in cache.");
        }
        else
        {
            Console.WriteLine($"{ticker}: {fundHistory.Prices.Count} records found in cache, {fundHistory.Prices[0].DateTime:yyyy-MM-dd} to {fundHistory.Prices[fundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}");
        }

        FundHistory? missingFundHistory;

        if (fundHistory == null)
        {
            Console.WriteLine($"{ticker}: Download entire history.");
            missingFundHistory = await FundHistoryDownloader.GetHistoryAll(ticker);
        }
        else
        {
            Console.WriteLine($"{ticker}: Download missing history.");
            missingFundHistory = await FundHistoryDownloader.GetMissingHistory(fundHistory, out DateTime missingStart, out DateTime missingEnd);
            Console.WriteLine($"{ticker}: Missing history identified as {missingStart::yyyy-MM-dd} to {missingEnd:yyyy-MM-dd}");
        }

        if (missingFundHistory == null)
        {
            Console.WriteLine($"{ticker}: No history missing.");
            continue;
        }

        Console.WriteLine($"{ticker}: Saving missing history, {missingFundHistory.Prices.Count} record(s), {missingFundHistory.Prices[0].DateTime:yyyy-MM-dd} to {missingFundHistory.Prices[missingFundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}, to cache.");
        await cache.Put(missingFundHistory);
    }
}

static HashSet<string> GetFundTickers()
{
    HashSet<SortedSet<string>> funds = [
        ["VTSMX", "VTI"],       // US TSM
        ["VFINX", "VOO"],       // US LCB
        ["DFSVX", "AVUV"],      // US SCV
        ["DFALX", "AVDE"],      // Int'l TSM
        ["DFIVX", "AVIV"],      // Int'l LCV
        ["DISVX", "AVDV"],      // Int'l SCV
        ["VEIEX", "AVEM"],      // EM
        ["DFEVX", "AVES"],      // EM LCV
        ["DEMSX", "AVEE"]       // EM SCB
    ];

    return funds.SelectMany(set => set).ToHashSet<string>();
}
await RefreshFundHistoryCache(new FundHistoryRepository());

static HashSet<string> GetFundTickers() => new HashSet<SortedSet<string>>([
        ["VTSMX", "VTI"],       // US TSM
        ["VFINX", "VOO"],       // US LCB
        ["DFLVX", "AVLV"],      // US LCV
        ["VIGAX"],              // US LCG
        [/* ? */ "VSMAX"],      // US SCB
        ["DFSVX", "AVUV"],      // US SCV
        ["VSGAX"],              // US SCG
        ["DFALX", "AVDE"],      // Int'l TSM/LCB
        ["DFIVX", "AVIV"],      // Int'l LCV
        [/* ? */ "EFG"],        // Int'l LCG
        //["?"],                // Int'l SCB
        ["DISVX", "AVDV"],      // Int'l SCV
        [/* ? */ "DISMX"],      // Int'l SCG
        ["VEIEX", "AVEM"],      // EM TSM/LCB
        ["DFEVX", "AVES"],      // EM LCV
        [/* ? */ "XSOE"],       // EM LCG
        ["DEMSX", "AVEE"],      // EM SCB
        ["DGS"],                // EM SCV
        //["?"]                 // EM SCG
    ])
    .SelectMany(set => set).ToHashSet<string>();

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
            Console.WriteLine($"{ticker}: {fundHistory.Prices.Count} record(s) in cache, {fundHistory.Prices[0].DateTime:yyyy-MM-dd} to {fundHistory.Prices[fundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}.");
        }

        FundHistory? missingFundHistory;

        if (fundHistory == null)
        {
            Console.WriteLine($"{ticker}: Download entire history.");

            missingFundHistory = await FundHistoryDownloader.GetHistoryAll(ticker);
        }
        else
        {
            missingFundHistory = await FundHistoryDownloader.GetMissingHistory(fundHistory, out DateTime missingStart, out DateTime missingEnd);

            if (missingFundHistory != null)
            {
                Console.WriteLine($"{ticker}: Missing history identified as {missingStart:yyyy-MM-dd} to {missingEnd:yyyy-MM-dd}");
            }
        }

        if (missingFundHistory == null)
        {
            Console.WriteLine($"{ticker}: No history missing.");
            continue;
        }

        Console.WriteLine($"{ticker}: Save {missingFundHistory.Prices.Count} new record(s), {missingFundHistory.Prices[0].DateTime:yyyy-MM-dd} to {missingFundHistory.Prices[missingFundHistory.Prices.Count - 1].DateTime:yyyy-MM-dd}.");

        await cache.Put(missingFundHistory);
    }
}
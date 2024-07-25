var fundRepository = new FundHistoryQuoteRepository("../../../data/quotes/");


await FundHistorySyntheticReturnsController.RefreshFundHistorySyntheticReturns("../../../source/Stock-Index-Data-20220923-Monthly.csv", "../../../data/returns/monthly/");
await FundHistoryQuotesController.RefreshFundHistoryQuotes(fundRepository, GetFundTickers());
await FundHistoryReturnsController.RefreshFundHistoryReturns(fundRepository, "../../../data/returns/");

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
.SelectMany(set => set)
.ToHashSet<string>();
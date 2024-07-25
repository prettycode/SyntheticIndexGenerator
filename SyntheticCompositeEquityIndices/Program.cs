var rootPath = "../../../..";
var fundRepositorySourcePath = Path.Combine(rootPath, "./FundHistoryCache/data/");
var syntheticUsSourcePath = Path.Combine(rootPath, "./SyntheticUsEquityIndices/source/Stock-Index-Data-20220923-Monthly.csv");
var syntheticUsSavePath = Path.Combine(rootPath, "./SyntheticUsEquityIndices/data/monthly/");
var fundHistoryReturnsSavePath = Path.Combine(rootPath, "./FundHistoryReturns/data/");

var fundRepository = new QuoteRepository(fundRepositorySourcePath);

await Task.WhenAll(
    SyntheticReturnsController.RefreshSyntheticReturns(syntheticUsSourcePath, syntheticUsSavePath),
    QuotesController.RefreshQuotes(fundRepository, GetFundTickers())
);

await ReturnsController.RefreshReturns(fundRepository, fundHistoryReturnsSavePath);

GetSyntheticCompositeEquityIndexDefinitions().ToList().ForEach(index => Console.WriteLine($"Index: {index.Ticker}"));

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

static HashSet<MarketIndex> GetSyntheticCompositeEquityIndexDefinitions() =>
[
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend, History = ["$TSM", "VTSMX", "VTI"]  },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend, History = ["$LCB", "VFINX", "VOO"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value, History = ["$LCV", "DFLVX", "AVLV"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth, History = ["$LCG", "VIGAX"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend, History = ["$MCB", /*"?"*/] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value, History = ["$MCV", /*"?"*/] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth, History = ["MSCG", /*"?"*/] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend, History = ["$SCB", "VSMAX"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value, History = ["$SCV", "DFSVX", "AVUV"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth, History = ["$SCG", "VSGAX"] },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth }
];
var quotesPath = "../../../data/quotes/";
var syntheticReturnsFilePath = "../../../source/Stock-Index-Data-20220923-Monthly.csv";
var saveSyntheticReturnsPath = "../../../data/returns/";
var quoteTickers = GetIndexDefinitions().SelectMany(index => index.History ?? []).Where(ticker => !ticker.StartsWith("$")).ToHashSet<string>();

var fundRepository = new QuoteRepository(quotesPath);

await QuotesController.RefreshQuotes(fundRepository, quoteTickers);
await ReturnsController.RefreshReturns(fundRepository, syntheticReturnsFilePath, saveSyntheticReturnsPath);
await IndicesController.RefreshIndices(saveSyntheticReturnsPath);


static HashSet<MarketIndex> GetIndexDefinitions() =>
[
    // U.S.

    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend, History = ["$TSM", "VTSMX", "VTI"]  },

    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend, History = ["$LCB", "VFINX", "VOO"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value, History = ["$LCV", "DFLVX", "AVLV"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth, History = ["$LCG", "VIGAX"] },

    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend, History = ["$MCB", /*"?"*/] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value, History = ["$MCV", /*"?"*/] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth, History = ["$MCG", /*"?"*/] },

    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend, History = ["$SCB", "VSMAX"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value, History = ["$SCV", "DFSVX", "AVUV"] },
    new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth, History = ["$SCG", "VSGAX"] },

    // Int'l

    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend, History = ["DFALX", "AVDE"] },

    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend, History = ["DFALX", "AVDE"] },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value, History = ["DFIVX", "AVIV"] },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth, History = [/* ? */ "EFG"] },

    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth },

    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value, History =  ["DISVX", "AVDV"] },
    new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth, History = [/* ? */ "DISMX"]},

    // EM

    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend, History = ["VEIEX", "AVEM"] },

    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend, History = ["VEIEX", "AVEM"] },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value, History = ["DFEVX", "AVES"] },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth, History = ["XSOE"]  },

    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth },

    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend, History = ["DEMSX", "AVEE"] },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value, History = ["DGS"] },
    new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth }
];
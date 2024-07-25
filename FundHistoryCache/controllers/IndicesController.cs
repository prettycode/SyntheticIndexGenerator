public static class IndicesController
{
    public static HashSet<MarketIndex> GetIndices() => [
        // U.S.

        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["$TSM", "VTSMX", "VTI"]  },

        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["$LCB", "VFINX", "VOO"] },
        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value, BackfillTickerSequence = ["$LCV", "DFLVX", "AVLV"] },
        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth, BackfillTickerSequence = ["$LCG", "VIGAX"] },

        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["$MCB", /*"?"*/] },
        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value, BackfillTickerSequence = ["$MCV", /*"?"*/] },
        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth, BackfillTickerSequence = ["$MCG", /*"?"*/] },

        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["$SCB", "VSMAX"] },
        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value, BackfillTickerSequence = ["$SCV", "DFSVX", "AVUV"] },
        new() { MarketRegion = MarketRegion.Us, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth, BackfillTickerSequence = ["$SCG", "VSGAX"] },

        // Int'l

        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["DFALX", "AVDE"] },

        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["DFALX", "AVDE"] },
        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value, BackfillTickerSequence = ["DFIVX", "AVIV"] },
        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth, BackfillTickerSequence = [/* ? */ "EFG"] },

        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend },
        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value },
        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth },

        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend },
        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value, BackfillTickerSequence =  ["DISVX", "AVDV"] },
        new() { MarketRegion = MarketRegion.InternationalDeveloped, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth, BackfillTickerSequence = [/* ? */ "DISMX"]},

        // EM

        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Total, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["VEIEX", "AVEM"] },

        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["VEIEX", "AVEM"] },
        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Value, BackfillTickerSequence = ["DFEVX", "AVES"] },
        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Large, MarketFactor = MarketFactor.Growth, BackfillTickerSequence = [/* ? */ "XSOE"]  },

        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Blend },
        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Value },
        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Mid, MarketFactor = MarketFactor.Growth },

        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Blend, BackfillTickerSequence = ["DEMSX", "AVEE"] },
        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Value, BackfillTickerSequence = ["DGS"] },
        new() { MarketRegion = MarketRegion.Emerging, MarketCap = MarketCap.Small, MarketFactor = MarketFactor.Growth }
    ];

    public static HashSet<string> GetBackfillTickers(bool filterSynthetic = true)
    {
        var indices = IndicesController.GetIndices().SelectMany(index => index.BackfillTickerSequence ?? []);

        if (!filterSynthetic)
        {
            return indices.ToHashSet();
        }

        return indices.Where(ticker => !ticker.StartsWith("$")).ToHashSet();
    }

    public static async Task RefreshIndices(string returnsPath)
    {

    }
}

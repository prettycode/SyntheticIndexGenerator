public static class IndicesController
{
    public static HashSet<Index> GetIndices() => [
        // U.S.

        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Total, Style = IndexStyle.Blend, BackfillTickerSequence = ["$TSM", "VTSMX", "VTI"]  },

        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Blend, BackfillTickerSequence = ["$LCB", "VFINX", "VOO"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Value, BackfillTickerSequence = ["$LCV", "DFLVX", "AVLV"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Growth, BackfillTickerSequence = ["$LCG", "VIGAX"] },

        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Blend, BackfillTickerSequence = ["$MCB", /*"?"*/] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Value, BackfillTickerSequence = ["$MCV", /*"?"*/] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Growth, BackfillTickerSequence = ["$MCG", /*"?"*/] },

        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Blend, BackfillTickerSequence = ["$SCB", "VSMAX"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Value, BackfillTickerSequence = ["$SCV", "DFSVX", "AVUV"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Growth, BackfillTickerSequence = ["$SCG", "VSGAX"] },

        // Int'l

        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Total, Style = IndexStyle.Blend, BackfillTickerSequence = ["DFALX", "AVDE"] },

        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Blend, BackfillTickerSequence = ["DFALX", "AVDE"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Value, BackfillTickerSequence = ["DFIVX", "AVIV"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Growth, BackfillTickerSequence = [/* ? */ "EFG"] },

        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Blend },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Value },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Growth },

        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Blend },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Value, BackfillTickerSequence =  ["DISVX", "AVDV"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Growth, BackfillTickerSequence = [/* ? */ "DISMX"]},

        // EM

        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Total, Style = IndexStyle.Blend, BackfillTickerSequence = ["VEIEX", "AVEM"] },

        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Blend, BackfillTickerSequence = ["VEIEX", "AVEM"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Value, BackfillTickerSequence = ["DFEVX", "AVES"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Growth, BackfillTickerSequence = [/* ? */ "XSOE"]  },

        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Blend },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Value },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Growth },

        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Blend, BackfillTickerSequence = ["DEMSX", "AVEE"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Value, BackfillTickerSequence = ["DGS"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Growth }
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
        ArgumentNullException.ThrowIfNull(returnsPath);
    }
}

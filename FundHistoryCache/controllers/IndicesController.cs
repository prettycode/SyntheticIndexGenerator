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

        return indices.Where(ticker => !ticker.StartsWith('$')).ToHashSet();
    }

    public static Task RefreshIndices(ReturnsRepository returnsCache)
    {
        async Task refreshIndex(string indexTicker, SortedSet<string> backfillTickers)
        {
            var collatedReturns = await IndicesController.CollateMostGranularReturns(returnsCache, backfillTickers);
            await returnsCache.Put(indexTicker, collatedReturns.returns, collatedReturns.granularity);
        }

        var backfillTickersByIndexTicker = IndicesController
            .GetIndices()
            .Where(index => index.BackfillTickerSequence != null)
            .ToDictionary(index => index.Ticker, index => index.BackfillTickerSequence);

        return Task.WhenAll(backfillTickersByIndexTicker.Select(pair => refreshIndex(pair.Key, pair.Value)));
    }

    private async static Task<(ReturnPeriod granularity, List<PeriodReturn> returns)> CollateMostGranularReturns(ReturnsRepository returnsCache, SortedSet<string> backfillTickers)
    {
        var result = new List<PeriodReturn>();
        var firstBackfillTicker = backfillTickers.First();
        var firstBackfillReturns = await returnsCache.GetMostGranular(firstBackfillTicker, out ReturnPeriod backfillGranularity);
        var remainingBackfillReturns = await Task.WhenAll(backfillTickers.Select(ticker => returnsCache.Get(ticker, backfillGranularity)));
        var backfillReturns = new[] { firstBackfillReturns }.Concat(remainingBackfillReturns).ToList();

        for (var i = 0; i < backfillTickers.Count; i++)
        {
            var currentTickerReturns = backfillReturns[i];
            int nextTickerIndex = i + 1;
            string nextTicker;
            List<PeriodReturn> nextTickerReturns;
            DateTime startDateOfNextTicker = DateTime.MaxValue;

            if (nextTickerIndex <= backfillTickers.Count - 1)
            {
                nextTicker = backfillTickers.ElementAt(nextTickerIndex);
                nextTickerReturns = backfillReturns[nextTickerIndex];
                startDateOfNextTicker = nextTickerReturns.First().PeriodStart;
            }

            result.AddRange(currentTickerReturns.TakeWhile(pair => pair.PeriodStart < startDateOfNextTicker));
        }

        return (backfillGranularity, result);
    }

    private async static Task<List<PeriodReturn>> CollateCompositeReturns(ReturnsRepository returnsCache, SortedSet<string> backfillTickers)
    {
        throw new NotImplementedException();

        List<PeriodReturn> result = [];
        Dictionary<string, KeyValuePair<ReturnPeriod, List<PeriodReturn>>> backfillReturns = [];

        foreach (var ticker in backfillTickers)
        {
            ReturnPeriod periodGranularity;
            var mostGranularReturns = await returnsCache.GetMostGranular(ticker, out periodGranularity);

            backfillReturns.Add(ticker, new KeyValuePair<ReturnPeriod, List<PeriodReturn>>(periodGranularity, mostGranularReturns));
        }

        for(var i = 0; i < backfillTickers.Count; i++)
        {
            var startDateOfNextTicker = DateTime.MaxValue;

            if (i + 1 < backfillTickers.Count - 1)
            {
                var nextTicker = backfillTickers.ElementAt(i + 1);
                var nextTickerPeriod = backfillReturns[nextTicker].Key;
                var nextTickerReturns = backfillReturns[nextTicker].Value;

                startDateOfNextTicker = nextTickerReturns.First().PeriodStart;
            }

        }

        return result;
    }
}

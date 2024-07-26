using YahooFinanceApi;

public static class IndicesController
{
    public static HashSet<Index> GetIndices() => [
        // U.S.

        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Total, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["$TSM", "VTSMX", "VTI", "AVUS"]  },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["$LCB", "VFINX", "VOO"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Value, OrderedBackfillTickerSequence = ["$LCV", "DFLVX", "AVLV"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Growth, OrderedBackfillTickerSequence = ["$LCG", "VIGAX"] },

        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["$MCB", "VIMAX", "AVMC"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Value, OrderedBackfillTickerSequence = ["$MCV", "VMVAX", "AVMV"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Growth, OrderedBackfillTickerSequence = ["$MCG", "VMGMX"] },

        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["$SCB", "VSMAX", "AVSC"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Value, OrderedBackfillTickerSequence = ["$SCV", "DFSVX", "AVUV"] },
        new() { Region = IndexRegion.Us, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Growth, OrderedBackfillTickerSequence = ["$SCG", "VSGAX"] },

        // Int'l

        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Total, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["DFALX", "AVDE"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["DFALX", "AVDE"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Value, OrderedBackfillTickerSequence = ["DFIVX", "AVIV"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Growth, OrderedBackfillTickerSequence = ["EFG"] },

        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Blend },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Value },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Growth },

        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["DFISX", "AVDS"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Value, OrderedBackfillTickerSequence =  ["DISVX", "AVDV"] },
        new() { Region = IndexRegion.InternationalDeveloped, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Growth, OrderedBackfillTickerSequence = ["DISMX"]},

        // EM

        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Total, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["VEIEX", "AVEM"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["VEIEX", "AVEM"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Value, OrderedBackfillTickerSequence = ["DFEVX", "AVES"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Large, Style = IndexStyle.Growth, OrderedBackfillTickerSequence = ["XSOE"]  },

        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Blend },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Value },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Mid, Style = IndexStyle.Growth },

        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Blend, OrderedBackfillTickerSequence = ["DEMSX", "AVEE"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Value, OrderedBackfillTickerSequence = ["DGS"] },
        new() { Region = IndexRegion.Emerging, MarketCap = IndexMarketCap.Small, Style = IndexStyle.Growth }
    ];

    public static HashSet<string> GetBackfillTickers(bool filterSynthetic = true)
    {
        var indices = IndicesController.GetIndices().SelectMany(index => index.OrderedBackfillTickerSequence ?? []);

        if (!filterSynthetic)
        {
            return indices.ToHashSet();
        }

        return indices.Where(ticker => !ticker.StartsWith('$')).ToHashSet();
    }

    public static Task RefreshIndices(ReturnsRepository returnsCache)
    {
        ArgumentNullException.ThrowIfNull(returnsCache);

        var refreshTasks = IndicesController
            .GetIndices()
            .Where(index => index.OrderedBackfillTickerSequence != null)
            .Select(index => IndicesController.RefreshIndex(returnsCache, index));

        return Task.WhenAll(refreshTasks);
    }

    private static async Task RefreshIndex(ReturnsRepository returnsCache, Index index)
    {
        async Task refreshIndex(string indexTicker, List<string> backfillTickers, ReturnPeriod period)
        {
            var returns = await IndicesController.CollateReturns(returnsCache, backfillTickers, period);
            await returnsCache.Put(indexTicker, returns, period);
        }

        var periods = Enum.GetValues<ReturnPeriod>();
        var ticker = index.Ticker;
        var backfillTickers = index.OrderedBackfillTickerSequence;
        var tasks = periods.Select(period => refreshIndex(ticker, backfillTickers, period));

        await Task.WhenAll(tasks);
    }

    private async static Task<List<PeriodReturn>> CollateReturns(ReturnsRepository returnsCache, List<string> backfillTickers, ReturnPeriod period)
    {
        var result = new List<PeriodReturn>();

        var availableBackfillTickers = backfillTickers.Where(ticker => returnsCache.Has(ticker, period)).ToList();
        var backfillReturns = await Task.WhenAll(availableBackfillTickers.Select(ticker => returnsCache.Get(ticker, period)));

        for (var i = 0; i < backfillReturns.Length; i++)
        {
            var currentTickerReturns = backfillReturns[i]!;
            int nextTickerIndex = i + 1;
            string nextTicker;
            List<PeriodReturn> nextTickerReturns;
            DateTime startDateOfNextTicker = DateTime.MaxValue;

            if (nextTickerIndex <= backfillReturns.Length - 1)
            {
                nextTicker = backfillTickers.ElementAt(nextTickerIndex);
                nextTickerReturns = backfillReturns[nextTickerIndex]!;
                startDateOfNextTicker = nextTickerReturns.First().PeriodStart;
            }

            result.AddRange(currentTickerReturns.TakeWhile(pair => pair.PeriodStart < startDateOfNextTicker));
        }

        return result;
    }

    private async static Task<List<PeriodReturn>> CollateCompositeReturns(ReturnsRepository returnsCache, List<string> backfillTickers)
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

using FundHistoryCache.Models;
using FundHistoryCache.Repositories;
using Index = FundHistoryCache.Models.Index;

namespace FundHistoryCache.Controllers
{
    public static class IndicesController
    {
        public static HashSet<Index> GetIndices() => [
            new (IndexRegion.Us, IndexMarketCap.Total, IndexStyle.Blend, ["$TSM", "VTSMX", "VTI", "AVUS"]),
            new (IndexRegion.Us, IndexMarketCap.Large, IndexStyle.Blend, ["$LCB", "VFINX", "VOO"]),
            new (IndexRegion.Us, IndexMarketCap.Large, IndexStyle.Value, ["$LCV", "DFLVX", "AVLV"]),
            new (IndexRegion.Us, IndexMarketCap.Large, IndexStyle.Growth, ["$LCG", "VIGAX"]),
            new (IndexRegion.Us, IndexMarketCap.Mid, IndexStyle.Blend, ["$MCB", "VIMAX", "AVMC"]),
            new (IndexRegion.Us, IndexMarketCap.Mid, IndexStyle.Value, ["$MCV", "VMVAX", "AVMV"]),
            new (IndexRegion.Us, IndexMarketCap.Mid, IndexStyle.Growth, ["$MCG", "VMGMX"]),
            new (IndexRegion.Us, IndexMarketCap.Small, IndexStyle.Blend, ["$SCB", "VSMAX", "AVSC"]),
            new (IndexRegion.Us, IndexMarketCap.Small, IndexStyle.Value, ["$SCV", "DFSVX", "AVUV"]),
            new (IndexRegion.Us, IndexMarketCap.Small, IndexStyle.Growth, ["$SCG", "VSGAX"]),
            new (IndexRegion.IntlDeveloped, IndexMarketCap.Total, IndexStyle.Blend, ["DFALX", "AVDE"]),
            new (IndexRegion.IntlDeveloped, IndexMarketCap.Large, IndexStyle.Blend, ["DFALX", "AVDE"]),
            new (IndexRegion.IntlDeveloped, IndexMarketCap.Large, IndexStyle.Value, ["DFIVX", "AVIV"]),
            new (IndexRegion.IntlDeveloped, IndexMarketCap.Large, IndexStyle.Growth, ["EFG"]),
            new (IndexRegion.IntlDeveloped, IndexMarketCap.Small, IndexStyle.Blend, ["DFISX", "AVDS"]),
            new (IndexRegion.IntlDeveloped, IndexMarketCap.Small, IndexStyle.Value, ["DISVX", "AVDV"]),
            new (IndexRegion.IntlDeveloped, IndexMarketCap.Small, IndexStyle.Growth, ["DISMX"]),
            new (IndexRegion.Emerging, IndexMarketCap.Total, IndexStyle.Blend, ["VEIEX", "AVEM"]),
            new (IndexRegion.Emerging, IndexMarketCap.Large, IndexStyle.Blend, ["VEIEX", "AVEM"]),
            new (IndexRegion.Emerging, IndexMarketCap.Large, IndexStyle.Value, ["DFEVX", "AVES"]),
            new (IndexRegion.Emerging, IndexMarketCap.Large, IndexStyle.Growth, ["XSOE"]),
            new (IndexRegion.Emerging, IndexMarketCap.Small, IndexStyle.Blend, ["DEMSX", "AVEE"]),
            new (IndexRegion.Emerging, IndexMarketCap.Small, IndexStyle.Value, ["DGS"])
        ];

        public static HashSet<string> GetBackfillTickers(bool filterSynthetic = true)
        {
            var indices = GetIndices().SelectMany(index => index.BackfillTickers ?? []);

            if (!filterSynthetic)
            {
                return indices.ToHashSet();
            }

            return indices.Where(ticker => !ticker.StartsWith('$')).ToHashSet();
        }

        public static Task RefreshIndices(ReturnsRepository returnsCache)
        {
            ArgumentNullException.ThrowIfNull(returnsCache);

            var refreshTasks =
                GetIndices()
                .Where(index => index.BackfillTickers != null)
                .Select(index => RefreshIndex(returnsCache, index));

            return Task.WhenAll(refreshTasks);
        }

        private static async Task RefreshIndex(ReturnsRepository returnsCache, Index index)
        {
            async Task refreshIndex(string indexTicker, List<string> backfillTickers, ReturnPeriod period)
            {
                var returns = await CollateReturns(returnsCache, backfillTickers, period);
                await returnsCache.Put(indexTicker, returns, period);
            }

            var periods = Enum.GetValues<ReturnPeriod>();
            var ticker = index.Ticker;
            var backfillTickers = index.BackfillTickers;
            var tasks = periods.Select(period => refreshIndex(ticker, backfillTickers, period));

            await Task.WhenAll(tasks);
        }
        private static async Task<List<PeriodReturn>> CollateReturns(ReturnsRepository returnsCache, List<string> backfillTickers, ReturnPeriod period)
        {
            var availableBackfillTickers = backfillTickers.Where(ticker => returnsCache.Has(ticker, period));
            var backfillReturns = await Task.WhenAll(availableBackfillTickers.Select(ticker => returnsCache.Get(ticker, period)));
            var collatedReturns = backfillReturns
                .Select((returns, index) =>
                    (returns, nextStartDate: index < backfillReturns.Length - 1
                        ? backfillReturns[index + 1]?.First().PeriodStart
                        : DateTime.MaxValue
                    )
                )
                .SelectMany(item => item.returns!.TakeWhile(pair => pair.PeriodStart < item.nextStartDate));

            return collatedReturns.ToList();
        }

        /*private async static Task<List<PeriodReturn>> CollateReturns(ReturnsRepository returnsCache, List<string> backfillTickers, ReturnPeriod period)
        {
            var result = new List<PeriodReturn>();

            var availableBackfillTickers = backfillTickers.Where(ticker => returnsCache.Has(ticker, period)).ToList();
            var backfillReturns = await Task.WhenAll(availableBackfillTickers.Select(ticker => returnsCache.Get(ticker, period)));

            for (var i = 0; i < backfillReturns.Length; i++)
            {
                var currentTickerReturns = backfillReturns[i]!;
                int nextTickerIndex = i + 1;
                List<PeriodReturn> nextTickerReturns;
                DateTime startDateOfNextTicker = DateTime.MaxValue;

                if (nextTickerIndex <= backfillReturns.Length - 1)
                {
                    nextTickerReturns = backfillReturns[nextTickerIndex]!;
                    startDateOfNextTicker = nextTickerReturns.First().PeriodStart;
                }

                result.AddRange(currentTickerReturns.TakeWhile(pair => pair.PeriodStart < startDateOfNextTicker));
            }

            return result;
        }*/
    }
}
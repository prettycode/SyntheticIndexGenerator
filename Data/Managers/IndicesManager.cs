using Data.Models;
using Data.Repositories;
using Microsoft.Extensions.Logging;
using Index = Data.Models.Index;

namespace Data.Controllers
{
    public class IndicesManager(IReturnRepository returnRepository, ILogger<IndicesManager> logger)
    {
        private IReturnRepository ReturnCache { get; init; } = returnRepository;

        private ILogger<IndicesManager> Logger { get; init; } = logger;

        public Task RefreshIndices()
        {
            var refreshTasks = GetIndices()
                .Where(index => index.BackfillTickers != null)
                .Select(index => RefreshIndex(index));

            return Task.WhenAll(refreshTasks);
        }

        public static HashSet<string> GetBackfillTickers(bool filterSynthetic = true)
        {
            var indices = GetIndices().SelectMany(index => index.BackfillTickers ?? []);

            if (!filterSynthetic)
            {
                return indices.ToHashSet();
            }

            return indices.Where(ticker => !ticker.StartsWith('$')).ToHashSet();
        }

        private static HashSet<Index> GetIndices() => [
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
            new (IndexRegion.Emerging, IndexMarketCap.Total, IndexStyle.Blend, ["DFEMX", "AVEM"]),
            new (IndexRegion.Emerging, IndexMarketCap.Large, IndexStyle.Blend, ["DFEMX", "AVEM"]),
            new (IndexRegion.Emerging, IndexMarketCap.Large, IndexStyle.Value, ["DFEVX", "AVES"]),
            new (IndexRegion.Emerging, IndexMarketCap.Large, IndexStyle.Growth, ["XSOE"]),
            new (IndexRegion.Emerging, IndexMarketCap.Small, IndexStyle.Blend, ["DEMSX", "AVEE"]),
            new (IndexRegion.Emerging, IndexMarketCap.Small, IndexStyle.Value, ["DGS"])
        ];

        private Task RefreshIndex(Index index)
        {
            var periods = Enum.GetValues<PeriodType>();
            var tasks = periods.Select(async period =>
            {
                var returns = await CollateReturnsA(index.BackfillTickers, period);
                await ReturnCache.Put(index.Ticker, returns, period);
            });

            return Task.WhenAll(tasks);
        }

        // TODO test
        private async Task<List<PeriodReturn>> CollateReturnsA(List<string> backfillTickers, PeriodType period)
        {
            var availableBackfillTickers = backfillTickers.Where(ticker => ReturnCache.Has(ticker, period));
            var backfillReturns = await Task.WhenAll(availableBackfillTickers.Select(ticker => ReturnCache.Get(ticker, period)));
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

        // TODO test
        private async Task<List<PeriodReturn>> CollateReturnsB(List<string> backfillTickers, PeriodType period)
        {
            var collatedReturns = new List<PeriodReturn>();
            var availableBackfillTickers = backfillTickers.Where(ticker => ReturnCache.Has(ticker, period));
            var backfillReturns = await Task.WhenAll(availableBackfillTickers.Select(ticker => ReturnCache.Get(ticker, period)));

            for (var i = 0; i < backfillReturns.Length; i++)
            {
                var currentTickerReturns = backfillReturns[i]!;
                int nextTickerIndex = i + 1;
                DateTime startDateOfNextTicker = DateTime.MaxValue;

                if (nextTickerIndex < backfillReturns.Length)
                {
                    var nextTickerReturns = backfillReturns[nextTickerIndex]!;
                    startDateOfNextTicker = nextTickerReturns.First().PeriodStart;
                }

                collatedReturns.AddRange(currentTickerReturns.TakeWhile(pair => pair.PeriodStart < startDateOfNextTicker));
            }

            return collatedReturns;
        }
    }
}
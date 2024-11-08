namespace Data.SyntheticIndices;

internal class SyntheticIndicesService(/*ILogger<SyntheticIndicesService> logger*/) : ISyntheticIndicesService
{
    public class Index(IndexRegion region, IndexMarketCap marketCap, IndexStyle style, List<string> backfillTickers)
    {
        public IndexRegion Region { get; set; } = region;

        public IndexMarketCap MarketCap { get; set; } = marketCap;

        public IndexStyle Style { get; set; } = style;

        public List<string> BackfillTickers { get; set; } = backfillTickers ?? throw new ArgumentNullException(nameof(backfillTickers));

        public string Ticker
        {
            get
            {
                var regionDesignation = Region switch
                {
                    IndexRegion.Us => "US",
                    IndexRegion.IntlDeveloped => "I",
                    IndexRegion.Emerging => "EM",
                    _ => throw new NotImplementedException(),
                };

                var marketCapDesignation = MarketCap switch
                {
                    IndexMarketCap.Total => "TSM",
                    IndexMarketCap.Large => "LC",
                    IndexMarketCap.Mid => "MC",
                    IndexMarketCap.Small => "SC",
                    _ => throw new NotImplementedException(),
                };

                var marketFactorDesignation = Style switch
                {
                    IndexStyle.Blend => "B",
                    IndexStyle.Value => "V",
                    IndexStyle.Growth => "G",
                    _ => throw new NotImplementedException(),
                };

                if (MarketCap == IndexMarketCap.Total)
                {
                    marketFactorDesignation = string.Empty;
                }

                return $"$^{regionDesignation}{marketCapDesignation}{marketFactorDesignation}";
            }
        }
    }

    public enum IndexMarketCap
    {
        Total,
        Large,
        Mid,
        Small
    }

    public enum IndexRegion
    {
        Us,
        IntlDeveloped,
        Emerging
    }

    public enum IndexStyle
    {
        Blend,
        Value,
        Growth
    }

    public HashSet<string> GetSyntheticIndexTickers() => GetIndices().Select(index => index.Ticker).ToHashSet();

    public HashSet<string> GetSyntheticIndexBackfillTickers(string syntheticIndexTicker, bool filterSynthetic = true)
        => GetIndices()
            .Single(index => index.Ticker == syntheticIndexTicker)
            .BackfillTickers
            .Where(backfillTicker => !filterSynthetic || !backfillTicker.StartsWith('$'))
            .ToHashSet();

    private static HashSet<Index> GetIndices() => [
        new (IndexRegion.Us, IndexMarketCap.Total, IndexStyle.Blend, ["$USTSM", "VTSMX", "VTI", "AVUS"]),
        new (IndexRegion.Us, IndexMarketCap.Large, IndexStyle.Blend, ["$SPYTR", /*"$USLCB",*/ "VFINX", "VOO"]),
        new (IndexRegion.Us, IndexMarketCap.Large, IndexStyle.Value, ["$USLCV", "DFLVX", "AVLV"]),
        new (IndexRegion.Us, IndexMarketCap.Large, IndexStyle.Growth, ["$USLCG", "VIGAX"]),
        new (IndexRegion.Us, IndexMarketCap.Mid, IndexStyle.Blend, ["$USMCB", "VIMAX", "AVMC"]),
        new (IndexRegion.Us, IndexMarketCap.Mid, IndexStyle.Value, ["$USMCV", "DFVEX", "AVMV"]),
        new (IndexRegion.Us, IndexMarketCap.Mid, IndexStyle.Growth, ["$USMCG", "VMGMX"]),
        new (IndexRegion.Us, IndexMarketCap.Small, IndexStyle.Blend, ["$USSCB", "VSMAX", "AVSC"]),
        new (IndexRegion.Us, IndexMarketCap.Small, IndexStyle.Value, ["$USSCV", "DFSVX", "AVUV"]),
        new (IndexRegion.Us, IndexMarketCap.Small, IndexStyle.Growth, ["$USSCG", "VSGAX"]),
        new (IndexRegion.IntlDeveloped, IndexMarketCap.Total, IndexStyle.Blend, ["DFALX", "AVDE"]),
        new (IndexRegion.IntlDeveloped, IndexMarketCap.Large, IndexStyle.Blend, ["DFALX", "AVDE"]),
        new (IndexRegion.IntlDeveloped, IndexMarketCap.Large, IndexStyle.Value, ["DFIVX", "AVIV"]),
        new (IndexRegion.IntlDeveloped, IndexMarketCap.Large, IndexStyle.Growth, ["EFG"]),
        new (IndexRegion.IntlDeveloped, IndexMarketCap.Mid, IndexStyle.Value, ["DFVQX", "DXIV"]),
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
}
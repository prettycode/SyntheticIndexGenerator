using static Data.Models.SyntheticIndex;

namespace Data.Models
{
    public class SyntheticIndex(IndexRegion region, IndexMarketCap marketCap, IndexStyle style, List<string> backFillTickers)
    {

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

        public static HashSet<SyntheticIndex> GetSyntheticIndices() => [
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

        public static HashSet<string> GetBackFillTickers(bool filterSynthetic = true)
        {
            var indices = GetSyntheticIndices().SelectMany(index => index.BackFillTickers ?? []);

            if (!filterSynthetic)
            {
                return indices.ToHashSet();
            }

            return indices.Where(ticker => !ticker.StartsWith('$')).ToHashSet();
        }

        public static HashSet<string> GetSyntheticIndexTickers()
            => GetSyntheticIndices().Select(index => index.Ticker).ToHashSet();

        public IndexRegion Region { get; set; } = region;

        public IndexMarketCap MarketCap { get; set; } = marketCap;

        public IndexStyle Style { get; set; } = style;

        public List<string> BackFillTickers { get; set; } = backFillTickers
            ?? throw new ArgumentNullException(nameof(backFillTickers));

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

}

namespace FundHistoryCache.Models
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
}
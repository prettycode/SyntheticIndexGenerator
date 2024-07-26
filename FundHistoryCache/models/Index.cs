public struct Index
{
    public IndexRegion Region { get; set; }
    public IndexMarketCap MarketCap { get; set; }
    public IndexStyle Style { get; set; }

    public List<string> OrderedBackfillTickerSequence { get; set; }

    public readonly string Ticker
    {
        get
        {
            var regionDesignation = this.Region switch
            {
                IndexRegion.Us => "US",
                IndexRegion.InternationalDeveloped => "I",
                IndexRegion.Emerging => "EM",
                _ => throw new NotImplementedException(),
            };

            var marketCapDesignation = this.MarketCap switch
            {
                IndexMarketCap.Total => "TSM",
                IndexMarketCap.Large => "LC",
                IndexMarketCap.Mid => "MC",
                IndexMarketCap.Small => "SC",
                _ => throw new NotImplementedException(),
            };

            var marketFactorDesignation = this.Style switch
            {
                IndexStyle.Blend => "B",
                IndexStyle.Value => "V",
                IndexStyle.Growth => "G",
                _ => throw new NotImplementedException(),
            };

            if (this.MarketCap == IndexMarketCap.Total)
            {
                marketFactorDesignation = string.Empty;
            }

            return $"$^{regionDesignation}{marketCapDesignation}{marketFactorDesignation}";
        }
    }
}
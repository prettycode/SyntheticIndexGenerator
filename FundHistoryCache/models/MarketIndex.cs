public struct MarketIndex
{
    public MarketRegion MarketRegion { get; set; }
    public MarketCap MarketCap { get; set; }
    public MarketFactor MarketFactor { get; set; }

    public SortedSet<string> History { get; set; }

    public readonly string Ticker
    {
        get
        {
            var regionDesignation = this.MarketRegion switch
            {
                MarketRegion.Us => "US",
                MarketRegion.InternationalDeveloped => "I",
                MarketRegion.Emerging => "EM",
                _ => throw new NotImplementedException(),
            };

            var marketCapDesignation = this.MarketCap switch
            {
                MarketCap.Total => "TSM",
                MarketCap.Large => "LC",
                MarketCap.Mid => "MC",
                MarketCap.Small => "SC",
                _ => throw new NotImplementedException(),
            };

            var marketFactorDesignation = this.MarketFactor switch
            {
                MarketFactor.Blend => "B",
                MarketFactor.Value => "V",
                MarketFactor.Growth => "G",
                _ => throw new NotImplementedException(),
            };

            if (this.MarketCap == MarketCap.Total)
            {
                marketFactorDesignation = string.Empty;
            }

            return $"${regionDesignation}{marketCapDesignation}{marketFactorDesignation}";
        }
    }
}
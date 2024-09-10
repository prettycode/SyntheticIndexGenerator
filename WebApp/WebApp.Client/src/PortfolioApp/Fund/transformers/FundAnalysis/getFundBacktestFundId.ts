import { Fund } from '../../models/Fund/Fund';

// TODO:
// This is dumb. Instead, take fundId and get backtest ID, then take marketRegion, assetClass, etc. from backtest fund
export const getFundBacktestFundDetails = (fundId: string): Partial<Fund> => {
    switch (fundId) {
        // US Total market
        case 'VTI':
        case 'ITOT':
        case 'AVUS':
        case 'VTSMX':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // S&P 500
        case 'VOO':
        case 'SPY':
        case 'VFINX':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // US Large-Cap Utilities
        case 'VPU':
        case 'XLU':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // US Large-Cap Consumer Staples
        case 'VDC':
        case 'XLP':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // US Large-Cap Healthcare
        case 'VHT':
        case 'XLV':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // US Large-Cap Real Estate
        case 'VNQ':
        case 'IYR':
        case 'VGSIX':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // Int’l Developed Large Cap
        case 'VEA':
        case 'EFA':
        case 'AVDE':
        case 'VTMGX':
        case 'DFALX':
            return { marketRegion: 'International Developed', assetClass: 'Equity' };

        // Emerging Markets
        case 'VWO':
        case 'EEM':
        case 'AVEM':
        case 'VEIEX':
            return { marketRegion: 'Emerging', assetClass: 'Equity' };

        // US Cash (Money Market)
        case 'USFR':
        case 'BIL':
        case 'SGOV':
        case 'TFLO':
        case 'FLOT':
            return { marketRegion: 'U.S.', assetClass: 'Cash' };

        // US Short-Term Treasuries
        case 'VGSH':
        case 'SHV':
        case 'VFISX':
            return { marketRegion: 'U.S.', assetClass: 'Treasury' };

        // US Intermediate-Term Treasuries
        case 'VGIT':
        case 'IEF':
        case 'VFITX':
            return { marketRegion: 'U.S.', assetClass: 'Treasury' };

        // US Long-Term Treasuries
        case 'VGLT':
        case 'TLT':
        case 'VUSTX':
            return { marketRegion: 'U.S.', assetClass: 'Treasury' };

        // US 25+ Year Treasuries
        case 'EDV':
        case 'ZROZ':
        case 'GOVZ':
        case 'VEDTX':
            return { marketRegion: 'U.S.', assetClass: 'Treasury' };

        case 'VBMFX':
        case 'AGG':
            return { marketRegion: 'U.S.', assetClass: 'Bond' };

        // Gold
        case 'GLD':
        case 'GLDM':
        case 'IAU':
        case 'IAUM':
        case 'SGOL':
        case 'AAAU':
        case 'OUNZ':
        case 'BAR':
        case 'PHYS':
            return { marketRegion: 'Global (All-World)', assetClass: 'Commodity' };

        // US Large-Cap Value
        case 'AVLV':
        case 'DFLV':
        case 'DFLVX':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // Int’l Developed Large-Cap Value
        case 'AVIV':
        case 'DFIV':
        case 'DFIVX':
            return { marketRegion: 'International Developed', assetClass: 'Equity' };

        // US Small-Cap Value
        case 'AVUV':
        case 'DFSV':
        case 'DFSVX':
            return { marketRegion: 'U.S.', assetClass: 'Equity' };

        // Int’l Developed Small-Cap Value
        case 'AVDV':
        case 'DISV':
        case 'DISVX':
            return { marketRegion: 'International Developed', assetClass: 'Equity' };

        // Emerging Markets Value
        case 'AVES':
        case 'DFEV':
        case 'DFEVX':
            return { marketRegion: 'Emerging', assetClass: 'Equity' };

        // Emerging Markets Small Cap Value
        case 'DGS':
        case 'DEMSX':
            return { marketRegion: 'Emerging', assetClass: 'Equity' };

        case 'ASFYX':
        case 'DBMF':
        case 'KMLM':
        case 'PQTIX':
            return { marketRegion: 'Global (All-World)', assetClass: 'Trend' };

        case 'NTSX':
        case 'Custom:NTSX':
            return { marketRegion: 'U.S.' };

        case 'NTSI':
        case 'Custom:NTSI':
            return {};

        case 'NTSE':
        case 'Custom:NTSE':
            return {};

        case 'GDE':
        case 'Custom:GDE':
            return { marketRegion: 'U.S.' };

        case 'SWAN':
        case 'Custom:SWAN':
            return { marketRegion: 'U.S.' };

        case 'AOR':
        case 'Custom:AOR':
            return { marketRegion: 'Global (All-World)' };

        default:
            return {};
    }
};

/**
 * Given the ticker symbol of an asset, return the ticker symbol of a similar asset with longer history.
 */
export const getFundBacktestFundId = (tickerSymbol: string): string => {
    switch (tickerSymbol) {
        // US Total market
        case 'VTI':
        case 'ITOT':
        case 'AVUS':
            return 'VTSMX';

        // S&P 500
        case 'VOO':
        case 'SPY':
            return 'VFINX';

        // US Large-Cap Utilities
        case 'VPU':
            return 'XLU';

        // US Large-Cap Consumer Staples
        case 'VDC':
            return 'XLP';

        // US Large-Cap Healthcare
        case 'VHT':
            return 'XLV';

        // US Large-Cap Real Estate
        case 'VNQ':
        case 'IYR':
            return 'VGSIX';

        // Int’l Developed Large Cap
        case 'VEA':
        case 'EFA':
        case 'AVDE':
        case 'VTMGX':
            return 'DFALX';

        // Emerging Markets
        case 'VWO':
        case 'EEM':
        case 'AVEM':
            return 'VEIEX';

        // US Cash (Money Market)
        case 'USFR':
        case 'BIL':
        case 'SGOV':
        case 'TFLO':
        case 'FLOT':
            return 'CASHX';

        // US Short-Term Treasuries
        case 'VGSH':
        case 'SHV':
            return 'VFISX';

        // US Intermediate-Term Treasuries
        case 'VGIT':
        case 'IEF':
            return 'VFITX';

        // US Long-Term Treasuries
        case 'VGLT':
        case 'TLT':
            return 'VUSTX';

        // US 25+ Year Treasuries
        case 'EDV':
        case 'ZROZ':
        case 'GOVZ':
            return 'VEDTX';

        // Gold
        case 'GLD':
        case 'GLDM':
        case 'IAU':
        case 'IAUM':
        case 'SGOL':
        case 'AAAU':
        case 'OUNZ':
        case 'BAR':
        case 'PHYS':
            return '^GOLD';

        // US Large-Cap Value
        case 'AVLV':
        case 'DFLV':
            return 'DFLVX';

        // Int’l Developed Large-Cap Value
        case 'AVIV':
        case 'DFIV':
            return 'DFIVX';

        // US Small-Cap Value
        case 'AVUV':
        case 'DFSV':
            return 'DFSVX';

        // Int’l Developed Small-Cap Value
        case 'AVDV':
        case 'DISV':
            return 'DISVX';

        // Emerging Markets Value
        case 'AVES':
        case 'DFEV':
            return 'DFEVX';

        // Emerging Markets Small Cap Value
        case 'DGS':
            // Emerging Markets Small Cap Portfolio
            return 'DEMSX';

        case 'PQTIX':
        case 'DBMF':
        case 'KMLM':
            return 'ASFYX';

        case 'NTSX':
            return 'Custom:NTSX';

        case 'NTSI':
            return 'Custom:NTSI';

        case 'NTSE':
            return 'Custom:NTSE';

        case 'GDE':
            return 'Custom:GDE';

        case 'SWAN':
            return 'Custom:SWAN';

        case 'AOR':
            return 'Custom:AOR';

        default:
            return tickerSymbol;
    }
};

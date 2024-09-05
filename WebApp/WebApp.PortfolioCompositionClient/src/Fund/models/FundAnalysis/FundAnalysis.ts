import { Fund } from '../Fund/Fund';
import { FundAllocation } from '../Fund/FundAllocation';
import { FundAssetClass } from '../Fund/FundAssetClass';
import { FundMarketRegion } from '../Fund/FundMarketRegion';

export type FundAnalysis = {
    holdings: Array<FundAllocation>;
    flattened: Array<FundAllocation>;
    leverage: number;
    delevered: Array<FundAllocation>;
    composition: Array<Fund>;
    decomposed: {
        assetClass: Record<FundAssetClass, Array<Fund>>;
        marketRegion: Record<FundMarketRegion, Array<Fund>>;
        assetByRegion: Partial<Record<FundAssetClass, Partial<Record<FundMarketRegion, Array<Fund>>>>>;
    };
};

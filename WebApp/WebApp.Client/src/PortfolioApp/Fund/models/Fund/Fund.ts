import { FundAllocation } from './FundAllocation';
import { FundAssetClass } from './FundAssetClass';
import { FundMarketRegion } from './FundMarketRegion';

export type Fund = FundAllocation & {
    tickerSymbol: string;
    name: string;
    description?: string;
    assetClass: FundAssetClass;
    type?: undefined | 'Custom';
    tickerSymbolBackTest?: string;
} & (
        | {
              allocations: FundAllocation[];
              marketRegion: undefined | FundMarketRegion;
          }
        | {
              allocations: FundAllocation[] & { length: 0 };
              marketRegion: FundMarketRegion;
          }
    );

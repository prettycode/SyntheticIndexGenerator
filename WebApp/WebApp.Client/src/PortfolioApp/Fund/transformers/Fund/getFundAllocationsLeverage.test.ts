import { FundAllocation } from '../../models/Fund/FundAllocation';
import { getFundAllocationsLeverage } from './getFundAllocationsLeverage';

describe('getFundAllocationsLeverage', () => {
    it('should throw an error if holdings do not add up to at least 100', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 10 },
            { fundId: 'VGIT', percentage: 20 },
            { fundId: 'GLD', percentage: -5 },
            { fundId: 'USFR', percentage: 15 }
        ];

        expect(() => getFundAllocationsLeverage(fundHoldings)).toThrow();
    });

    it('should calculate 1.05 leverage', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 50 },
            { fundId: 'VGIT', percentage: 50 },
            { fundId: 'GLD', percentage: -5 },
            { fundId: 'USFR', percentage: 5 }
        ];

        expect(getFundAllocationsLeverage(fundHoldings)).toBe(1.05);
    });

    it('should calculate 1.50 leverage', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 90 },
            { fundId: 'VGIT', percentage: 60 },
            { fundId: 'GLD', percentage: -50 }
        ];

        expect(getFundAllocationsLeverage(fundHoldings)).toBe(1.5);
    });

    it('should calculate 1.8 leverage', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 90 },
            { fundId: 'VGIT', percentage: 90 },
            { fundId: 'GLD', percentage: -80 }
        ];

        expect(getFundAllocationsLeverage(fundHoldings)).toBe(1.8);
    });

    it('should calculate 1.0 leverage', () => {
        const fundHoldings: Array<FundAllocation> = [{ fundId: 'SPY', percentage: 100 }];

        expect(getFundAllocationsLeverage(fundHoldings)).toBe(1.0);
    });

    it('should calculate 1.0 leverage', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 50 },
            { fundId: 'VGIT', percentage: 50 }
        ];

        expect(getFundAllocationsLeverage(fundHoldings)).toBe(1.0);
    });
});

import { FundAllocation } from '../../models/Fund/FundAllocation';
import { getDeleveredFundAllocations } from './getDeleveredFundAllocations';

describe('getDeleveredFundAllocations', () => {
    it('should throw an error if holdings are missing', () => {
        const fundHoldings: Array<FundAllocation> = [];
        expect(() => getDeleveredFundAllocations(fundHoldings)).toThrow();
    });

    it('should return correct unlevered portfolio of 60/40', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: -50 },
            { fundId: 'VGIT', percentage: 90 },
            { fundId: 'GLD', percentage: 60 }
        ];

        const expected: Array<FundAllocation> = [
            { fundId: 'VGIT', percentage: 60 },
            { fundId: 'GLD', percentage: 40 }
        ];

        expect(getDeleveredFundAllocations(fundHoldings)).toEqual(expected);
    });

    it('should return correct unlevered portfolio of 50/50', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 90 },
            { fundId: 'VGIT', percentage: -80 },
            { fundId: 'GLD', percentage: 90 }
        ];

        const expected: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 50 },
            { fundId: 'GLD', percentage: 50 }
        ];

        expect(getDeleveredFundAllocations(fundHoldings)).toEqual(expected);
    });

    it('should return correct unlevered portfolio of 60/40', () => {
        const fundHoldings: Array<FundAllocation> = [
            { fundId: 'VGIT', percentage: 40 },
            { fundId: 'SPY', percentage: 60 }
        ];

        const expected: Array<FundAllocation> = [
            { fundId: 'SPY', percentage: 60 },
            { fundId: 'VGIT', percentage: 40 }
        ];

        expect(getDeleveredFundAllocations(fundHoldings)).toEqual(expected);
    });

    it('should return correct unlevered portfolio of 100', () => {
        const fundHoldings: Array<FundAllocation> = [{ fundId: 'SPY', percentage: 100 }];
        const expected: Array<FundAllocation> = [{ fundId: 'SPY', percentage: 100 }];

        expect(getDeleveredFundAllocations(fundHoldings)).toEqual(expected);
    });
});

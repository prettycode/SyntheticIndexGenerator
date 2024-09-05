import { FundAllocation } from '../../models/Fund/FundAllocation';

export const getFundAllocationsLeverage = (fundHoldings: Array<FundAllocation>): number => {
    if (!fundHoldings.length) {
        throw new Error('Fund is missing holdings. Cannot calculate leveraged.');
    }

    // TODO hack for floats
    const totalPercentage = +fundHoldings.reduce((sum, holding) => sum + holding.percentage, 0).toFixed(2);

    if (totalPercentage !== 100) {
        throw new Error(`Holdings added up to ${totalPercentage} instead of 100.`);
    }

    // TODO hack for floats
    const totalPercentageSansBarrowing = +fundHoldings
        .reduce((sum, holding) => sum + (holding.percentage < 0 ? 0 : holding.percentage), 0)
        .toFixed(2);

    if (totalPercentageSansBarrowing < 100) {
        throw new Error('Holdings must add up to 100 or more when negative-percentage holdings are removed.');
    }

    return totalPercentageSansBarrowing / 100;
};

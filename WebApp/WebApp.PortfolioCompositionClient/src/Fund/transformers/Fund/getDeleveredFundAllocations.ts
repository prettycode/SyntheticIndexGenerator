import { FundAllocation } from '../../models/Fund/FundAllocation';

export const getDeleveredFundAllocations = (fundHoldings: Array<FundAllocation>): Array<FundAllocation> => {
    if (!fundHoldings.length) {
        throw new Error('Fund is missing holdings. Cannot calculate unlevered portfolio.');
    }

    const leverage =
        fundHoldings.reduce((sum, holding) => sum + (holding.percentage < 0 ? 0 : holding.percentage), 0) / 100;

    const deleveredHoldings = fundHoldings
        .filter((holding) => holding.percentage >= 0)
        .map<FundAllocation>((holding) => ({
            fundId: holding.fundId,
            percentage: holding.percentage / leverage
        }))
        // Sort from highest to lowest percentage
        .sort((a, b) => b.percentage - a.percentage);

    return deleveredHoldings;
};

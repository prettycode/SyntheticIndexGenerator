import { FundAllocation } from '../../models/Fund/FundAllocation';
import { fetchFundByFundId } from '../../services/fetchFundByFundId';

const sortFundAllocationsDescending = (funds: Array<FundAllocation>) =>
    funds.sort((a, b) => b.percentage - a.percentage);

export const getFlattenedFundAllocations = async (funds: Array<FundAllocation>): Promise<Array<FundAllocation>> => {
    const flattened: Array<FundAllocation> = [];

    async function flatten(holdings: Array<FundAllocation>, weight: number) {
        for (const holding of holdings) {
            const holdingFund = await fetchFundByFundId(holding.fundId);

            if (holdingFund.allocations && holdingFund.allocations.length > 0) {
                await flatten(holdingFund.allocations, (holding.percentage * weight) / 100);
            } else {
                flattened.push({
                    fundId: holding.fundId,
                    percentage: (holding.percentage * weight) / 100
                });
            }
        }
    }

    await flatten(funds, 100);

    // TODO no unit tests written cover this yet
    // Collapsing duplicates and summing percentages
    const holdingsGrouped = flattened.reduce((acc: Array<FundAllocation>, curr: FundAllocation) => {
        const existingEntry = acc.find((entry) => entry.fundId === curr.fundId);

        if (existingEntry) {
            existingEntry.percentage += curr.percentage;
        } else {
            acc.push(curr);
        }

        return acc;
    }, []);
    /*.filter((holding) => holding.percentage !== 0)*/ // sort DESC
    return sortFundAllocationsDescending(holdingsGrouped);
};

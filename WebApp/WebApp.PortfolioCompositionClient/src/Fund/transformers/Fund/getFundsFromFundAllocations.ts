import cloneDeep from 'lodash.clonedeep';
import { Fund } from '../../models/Fund/Fund';
import { fetchFundByFundId } from '../../services/fetchFundByFundId';
import { FundAllocation } from '../../models/Fund/FundAllocation';

export const getFundFromFundAllocation = async (
    allocation: FundAllocation | Array<FundAllocation>
): Promise<Array<Fund>> => {
    const allocations = Array.isArray(allocation) ? allocation : [allocation];
    const allocationsAsFunds = await Promise.all(
        allocations.map(async (allocation) => {
            const fundDefinition: Fund = await fetchFundByFundId(allocation.fundId);
            const fund: Fund = {
                ...cloneDeep(fundDefinition),
                percentage: allocation.percentage
            };

            return fund;
        })
    );

    return allocationsAsFunds;
};

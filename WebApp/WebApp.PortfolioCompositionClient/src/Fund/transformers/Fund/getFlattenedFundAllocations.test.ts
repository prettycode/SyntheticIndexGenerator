import { Fund } from '../../models/Fund/Fund';
import { FundAllocation } from '../../models/Fund/FundAllocation';
import { fetchCustomFunds } from '../../services/fetchCustomFunds';
import { getFlattenedFundAllocations } from './getFlattenedFundAllocations';

const mockCustomFunds: Fund[] = [
    {
        fundId: 'Custom:NTSX',
        tickerSymbol: 'Custom:NTSX',
        percentage: 100,
        name: 'Market-cap Weighted All-World Equity',
        description: '',
        marketRegion: undefined,
        assetClass: undefined,
        allocations: [{ fundId: 'SPY', percentage: 100 }]
    },
    {
        fundId: 'Custom:GDE',
        tickerSymbol: 'Custom:GDE',
        percentage: 100,
        name: '60/20/20 All-World Equity',
        description: '',
        marketRegion: undefined,
        assetClass: undefined,
        allocations: [
            { fundId: 'VGIT', percentage: 60 },
            { fundId: 'GLD', percentage: 20 },
            { fundId: 'USFR', percentage: 20 }
        ]
    },
    {
        fundId: 'Custom:NTSI',
        tickerSymbol: 'Custom:NTSI',
        percentage: 100,
        name: '90/10 All-World Equity/Bonds',
        description: '',
        marketRegion: undefined,
        assetClass: undefined,
        allocations: [
            { fundId: 'Custom:GDE', percentage: 90 },
            { fundId: 'VEA', percentage: 10 }
        ]
    },
    {
        fundId: 'Custom:NTSE',
        tickerSymbol: 'Custom:NTSE',
        percentage: 100,
        name: '80% 90/10 All-World Equity/Bonds + 10% Gold',
        description: '',
        marketRegion: undefined,
        assetClass: undefined,
        allocations: [
            { fundId: 'Custom:NTSI', percentage: 80 },
            { fundId: 'VWO', percentage: 20 }
        ]
    },
    {
        fundId: '11',
        tickerSymbol: '11',
        percentage: 100,
        name: '50% Bonds + 50% Gold',
        description: '',
        marketRegion: undefined,
        assetClass: undefined,
        allocations: [
            { fundId: 'VEA', percentage: 50 },
            { fundId: 'VWO', percentage: 50 }
        ]
    },
    {
        fundId: '12',
        tickerSymbol: '12',
        percentage: 100,
        name: '50% All-World Equities + 50% Bonds/Gold Split',
        description: '',
        marketRegion: undefined,
        assetClass: undefined,
        allocations: [
            { fundId: 'Custom:GDE', percentage: 50 },
            { fundId: '11', percentage: 50 }
        ]
    },
    {
        fundId: 'SPY',
        percentage: 100,
        name: 'VT',
        marketRegion: 'Global (All-World)',
        assetClass: 'Equity',
        allocations: [],
        description: '',
        tickerSymbol: ''
    },
    {
        fundId: 'VGIT',
        percentage: 100,
        name: 'VTI',
        marketRegion: 'U.S.',
        assetClass: 'Equity',
        allocations: [],
        description: '',
        tickerSymbol: ''
    },
    {
        fundId: 'GLD',
        percentage: 100,
        name: 'VEA',
        marketRegion: 'International Developed',
        assetClass: 'Equity',
        allocations: [],
        description: '',
        tickerSymbol: ''
    },
    {
        fundId: 'USFR',
        percentage: 100,
        name: 'VWO',
        marketRegion: 'Emerging',
        assetClass: 'Equity',
        allocations: [],
        description: '',
        tickerSymbol: ''
    },
    {
        fundId: 'VEA',
        percentage: 100,
        name: 'BNDW',
        marketRegion: 'Global (All-World)',
        assetClass: 'Bond',
        allocations: [],
        description: '',
        tickerSymbol: ''
    },
    {
        fundId: 'VWO',
        percentage: 100,
        name: 'GLD',
        marketRegion: 'Global (All-World)',
        assetClass: 'Commodity',
        allocations: [],
        description: '',
        tickerSymbol: ''
    }
];

// TODO hack for float
const fundTotal = (fund: Array<FundAllocation>): number =>
    +fund.reduce((acc, curr) => acc + curr.percentage, 0).toFixed(2);

fetchCustomFunds.set(mockCustomFunds);

describe('getFlattenedFundAllocation', () => {
    it('should flatten custom fund of market fund into fund of only that market fund', async () => {
        const given = mockCustomFunds[0].allocations;
        const actual = await getFlattenedFundAllocations(given);
        const expected: Array<FundAllocation> = [
            {
                fundId: mockCustomFunds[6].fundId,
                percentage: 100
            }
        ];

        expect(actual).toEqual(expected);
        expect(fundTotal(actual)).toBe(100);
    });

    it('should flatten custom fund of market funds into fund of market funds', async () => {
        const given = mockCustomFunds[1].allocations;
        const actual = await getFlattenedFundAllocations(given);
        const expected: Array<FundAllocation> = [
            { fundId: 'VGIT', percentage: 60 },
            { fundId: 'GLD', percentage: 20 },
            { fundId: 'USFR', percentage: 20 }
        ];

        expect(actual).toEqual(expected);
        expect(fundTotal(actual)).toBe(100);
    });

    it('should flatten custom fund of market fund and custom fund into fund of market and custom fund', async () => {
        const given = mockCustomFunds[2].allocations;
        const actual = await getFlattenedFundAllocations(given);
        const expected: Array<FundAllocation> = [
            { fundId: 'VGIT', percentage: 60 * 0.9 },
            { fundId: 'GLD', percentage: 20 * 0.9 },
            { fundId: 'USFR', percentage: 20 * 0.9 },
            { fundId: 'VEA', percentage: 100 * 0.1 }
        ];

        expect(actual).toEqual(expected);
        expect(fundTotal(actual)).toBe(100);
    });

    it('should flatten custom fund of market fund and custom fund into fund of market and custom fund', async () => {
        const given = mockCustomFunds[3].allocations;
        const actual = await getFlattenedFundAllocations(given);
        const expected: Array<FundAllocation> = [
            { fundId: 'VGIT', percentage: 60 * 0.9 * 0.8 },
            { fundId: 'VWO', percentage: 20 * 1.0 * 1.0 },
            { fundId: 'GLD', percentage: 20 * 0.9 * 0.8 },
            { fundId: 'USFR', percentage: 20 * 0.9 * 0.8 },
            { fundId: 'VEA', percentage: 100 * 0.1 * 0.8 }
        ];

        expect(actual).toEqual(expected);
        expect(fundTotal(actual)).toBe(100);
    });

    it('should flatten custom fund of custom funds', async () => {
        const given = mockCustomFunds[5].allocations;
        const actual = await getFlattenedFundAllocations(given);
        const expected: Array<FundAllocation> = [
            { fundId: 'VGIT', percentage: 60 * 0.5 },
            { fundId: 'VEA', percentage: 50 * 0.5 },
            { fundId: 'VWO', percentage: 50 * 0.5 },
            { fundId: 'GLD', percentage: 20 * 0.5 },
            { fundId: 'USFR', percentage: 20 * 0.5 }
        ];

        expect(actual).toEqual(expected);
        expect(fundTotal(actual)).toBe(100);
    });
});

import Decimal from 'decimal.js';
import { Fund } from '../models/Fund/Fund';
import { fetchFundByFundId } from './fetchFundByFundId';

let fundList: Array<Fund> = [
    {
        fundId: 'Custom:CASH',
        percentage: 100,
        tickerSymbol: 'CASHX',
        name: 'U.S. Treasury Money Market (CASHX)',
        marketRegion: 'U.S.',
        assetClass: 'Cash',
        allocations: []
    },
    {
        fundId: 'Custom:GOLD',
        percentage: 100,
        tickerSymbol: 'GOLDX',
        name: 'Gold',
        marketRegion: 'Global (All-World)',
        assetClass: 'Commodity',
        allocations: []
    },
    {
        fundId: 'Custom:DBMF',
        percentage: 100,
        tickerSymbol: 'DBMFX',
        name: 'SG CTA Index (DBMF)',
        marketRegion: 'Global (All-World)',
        assetClass: 'Trend',
        allocations: []
    },
    {
        fundId: 'Custom:KMLM',
        percentage: 100,
        tickerSymbol: 'KMLMX',
        name: 'KFA MLM Index (KMLM)',
        marketRegion: 'Global (All-World)',
        assetClass: 'Trend',
        allocations: []
    },
    {
        fundId: 'Custom:SPY',
        percentage: 100,
        tickerSymbol: 'SPYTR',
        name: 'S&P 500 Index (SPY, VOO)',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:VTI',
        percentage: 100,
        tickerSymbol: 'VTSMX',
        name: 'Total U.S. Market (VTI, AVUS, DFAC)',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:VT',
        percentage: 100,
        tickerSymbol: 'VTSIM',
        name: 'Total World Market (VT, AVGE, DFAW)',
        assetClass: 'Equity',
        marketRegion: 'Global (All-World)',
        allocations: []
    },
    {
        fundId: 'Custom:VXUS',
        percentage: 100,
        tickerSymbol: 'VXUSX',
        name: 'Total Ex-U.S. Market (VXUS)',
        assetClass: 'Equity',
        marketRegion: 'Ex-U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:VEA',
        percentage: 100,
        tickerSymbol: 'DFALX',
        name: 'International Developed (VEA, AVDE, DFIC)',
        assetClass: 'Equity',
        marketRegion: 'International Developed',
        allocations: []
    },
    {
        fundId: 'Custom:VWO',
        percentage: 100,
        tickerSymbol: 'DFEMX',
        name: 'Emerging Markets (VWO, AVEM, DFEM)',
        assetClass: 'Equity',
        marketRegion: 'Emerging',
        allocations: []
    },
    {
        fundId: 'Custom:AVLV',
        percentage: 100,
        tickerSymbol: 'DFLVX',
        name: 'U.S. Large Cap Value (AVLV, DFLV)',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:AVUV',
        percentage: 100,
        tickerSymbol: 'DFSVX',
        name: 'U.S. Small Cap Value (AVUV, DFSV)',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:AVSC',
        percentage: 100,
        tickerSymbol: 'DFSTX',
        name: 'U.S. Small Cap (AVSC, DFAS)',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:AVDV',
        percentage: 100,
        tickerSymbol: 'DISVX',
        name: 'International Small Cap Value (AVDV, DISV)',
        assetClass: 'Equity',
        marketRegion: 'International Developed',
        allocations: []
    },
    {
        fundId: 'Custom:AVDS',
        percentage: 100,
        tickerSymbol: 'DFISX',
        name: 'International Small Cap (AVDS, DFIS)',
        assetClass: 'Equity',
        marketRegion: 'International Developed',
        allocations: []
    },
    {
        fundId: 'Custom:AVIV',
        percentage: 100,
        tickerSymbol: 'DFIVX',
        name: 'International Large Cap Value (AVIV, DFIV)',
        assetClass: 'Equity',
        marketRegion: 'International Developed',
        allocations: []
    },
    {
        fundId: 'Custom:AVEE',
        percentage: 100,
        tickerSymbol: 'DEMSX',
        name: 'Emerging Small Cap (AVEE, EEMS, EWX)',
        assetClass: 'Equity',
        marketRegion: 'Emerging',
        allocations: []
    },
    {
        fundId: 'Custom:AVES',
        percentage: 100,
        tickerSymbol: 'DFEVX',
        name: 'Emerging Value (AVES, DFEV)',
        assetClass: 'Equity',
        marketRegion: 'Emerging',
        allocations: []
    },
    {
        fundId: 'Custom:VNQ',
        percentage: 100,
        tickerSymbol: 'DFREX', // VGSIX
        name: 'U.S. Real Estate (VNQ, DFAR)',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:XLU',
        percentage: 100,
        tickerSymbol: 'XLU',
        name: 'U.S. Utilities',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:XLV',
        percentage: 100,
        tickerSymbol: 'XLV',
        name: 'U.S. Healthcare',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:XLP',
        percentage: 100,
        tickerSymbol: 'XLP',
        name: 'U.S. Staples',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:USDEF',
        type: 'Custom',
        percentage: 100,
        tickerSymbol: 'USDEFX',
        name: 'U.S. Defensive',
        description: '1:1:1 XLU:XLV:XLP',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: [
            { fundId: 'Custom:XLU', percentage: 33.34 },
            { fundId: 'Custom:XLV', percentage: 33.33 },
            { fundId: 'Custom:XLP', percentage: 33.33 }
        ]
    },
    {
        fundId: 'Custom:RSST',
        percentage: 100,
        tickerSymbol: 'RSST',
        name: '100/100 SPY/Trend (RSST)',
        description: '2x 50/50 SPY/Trend',
        marketRegion: undefined,
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:SPY',
                percentage: 100
            },
            {
                fundId: 'Custom:DBMF',
                percentage: 100
            },
            {
                fundId: 'Custom:CASH',
                percentage: -100
            }
        ]
    },
    {
        fundId: 'Custom:VGSH',
        percentage: 100,
        tickerSymbol: 'VFISX',
        name: 'U.S. Short-Term Treasury',
        marketRegion: 'U.S.',
        assetClass: 'Treasury',
        allocations: []
    },
    {
        fundId: 'Custom:VGIT',
        percentage: 100,
        tickerSymbol: 'VFITX',
        name: 'U.S. Intermediate-Term Treasury',
        marketRegion: 'U.S.',
        assetClass: 'Treasury',
        allocations: []
    },
    {
        fundId: 'Custom:VGLT',
        percentage: 100,
        tickerSymbol: 'VUSTX',
        name: 'U.S. Long-Term Treasury',
        marketRegion: 'U.S.',
        assetClass: 'Treasury',
        allocations: []
    },
    {
        fundId: 'Custom:ZROZ',
        percentage: 100,
        tickerSymbol: 'ZROZX',
        name: 'U.S. 25+ Year STRIPS Treasury',
        marketRegion: 'U.S.',
        assetClass: 'Treasury',
        allocations: []
    },
    {
        fundId: 'Custom:GOVT',
        percentage: 100,
        tickerSymbol: 'GOVT',
        name: '1:1:1 VGSH:VGIT:VGLT (GOVT)',
        description: 'U.S. Treasury Bond Ladder',
        marketRegion: 'U.S.',
        assetClass: 'Treasury',
        allocations: [
            {
                fundId: 'Custom:VGSH',
                percentage: 33.33
            },
            {
                fundId: 'Custom:VGIT',
                percentage: 33.34
            },
            {
                fundId: 'Custom:VGLT',
                percentage: 33.33
            }
        ]
    },
    {
        fundId: 'Custom:RSSB',
        percentage: 100,
        tickerSymbol: 'RSSB',
        name: '100/100 VT/GOVT (RSSB)',
        description: '2x 50/50 VT/U.S. Treasuries',
        marketRegion: 'U.S.',
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:VT',
                percentage: 100
            },
            {
                fundId: 'Custom:GOVT',
                percentage: 100
            },
            {
                fundId: 'Custom:CASH',
                percentage: -100
            }
        ]
    },
    {
        fundId: 'Custom:RSBT',
        percentage: 100,
        tickerSymbol: 'RSBT',
        name: '100/100 GOVT/Trend (RSBT)',
        description: '2x 50/50 U.S. Treasuries/Trend',
        marketRegion: undefined,
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:GOVT',
                percentage: 100
            },
            {
                fundId: 'Custom:DBMF',
                percentage: 100
            },
            {
                fundId: 'Custom:CASH',
                percentage: -100
            }
        ]
    },
    {
        fundId: 'Custom:NTSX',
        percentage: 100,
        tickerSymbol: 'NTSX',
        name: '90/60 SPY/GOVT (NTSX)',
        description: '1.5x 60/40 SPY/U.S. Treasuries',
        marketRegion: 'U.S.',
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:SPY',
                percentage: 90
            },
            {
                fundId: 'Custom:GOVT',
                percentage: 60
            },
            {
                fundId: 'Custom:CASH',
                percentage: -50
            }
        ]
    },
    {
        fundId: 'Custom:NTSI',
        percentage: 100,
        tickerSymbol: 'NTSI',
        name: '90/60 VEA/GOVT (NTSI)',
        description: '1.5x 60/40 VEA/U.S. Treasuries',
        marketRegion: undefined,
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:VEA',
                percentage: 90
            },
            {
                fundId: 'Custom:GOVT',
                percentage: 60
            },
            {
                fundId: 'Custom:CASH',
                percentage: -50
            }
        ]
    },
    {
        fundId: 'Custom:NTSE',
        percentage: 100,
        tickerSymbol: 'NTSE',
        name: '90/60 VWO/GOVT (NTSE)',
        description: '1.5x 60/40 VWO/U.S. Treasuries',
        marketRegion: undefined,
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:VWO',
                percentage: 90
            },
            {
                fundId: 'Custom:GOVT',
                percentage: 60
            },
            {
                fundId: 'Custom:CASH',
                percentage: -50
            }
        ]
    },
    {
        fundId: 'Custom:NTSWX',
        type: 'Custom',
        percentage: 100,
        tickerSymbol: 'World Efficient Core',
        name: '60/20/20 NTSX/NTSI/NTSE',
        description: '1.5x 60/40 VT/U.S. Treasuries',
        marketRegion: undefined,
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:NTSX',
                percentage: 60
            },
            {
                fundId: 'Custom:NTSI',
                percentage: 20
            },
            {
                fundId: 'Custom:NTSE',
                percentage: 20
            }
        ]
    },
    {
        fundId: 'Custom:GDE',
        percentage: 100,
        tickerSymbol: 'GDE',
        name: '90/90 SPY/GLD (GDE)',
        description: '1.8x 50/50 SPY/Gold',
        marketRegion: undefined,
        assetClass: 'Composite',
        allocations: [
            {
                fundId: 'Custom:SPY',
                percentage: 90
            },
            {
                fundId: 'Custom:GOLD',
                percentage: 90
            },
            {
                fundId: 'Custom:CASH',
                percentage: -80
            }
        ]
    },
    {
        fundId: 'Custom:SSO',
        percentage: 100,
        tickerSymbol: 'SPYTR?L=2',
        name: 'S&P 500 2x (SSO)',
        assetClass: 'Equity',
        marketRegion: 'U.S.',
        allocations: []
    },
    {
        fundId: 'Custom:AOR',
        percentage: 100,
        tickerSymbol: 'AOR',
        name: '60/40 (AOR)',
        assetClass: 'Composite',
        marketRegion: 'U.S.',
        allocations: [
            {
                fundId: 'Custom:VT',
                percentage: 60
            },
            {
                // BND not GOVT
                fundId: 'Custom:GOVT',
                percentage: 40
            }
        ]
    }
];

export const fetchCustomFunds = async (): Promise<Array<Fund>> => Promise.resolve(fundList);

fetchCustomFunds.set = (funds: Array<Fund>): void => {
    fundList = funds;
};

for (const { fundId, allocations } of fundList) {
    if (allocations.length === 0) {
        continue;
    }

    const allocationTotals = allocations.reduce((total, { fundId: allocationFundId, percentage }) => {
        if (!fetchFundByFundId(allocationFundId)) {
            throw new Error(
                `Fund "${fundId}" has allocation to "${allocationFundId}", but "${allocationFundId}" does not exist.`
            );
        }
        return total.plus(percentage);
    }, new Decimal(0));

    if (!allocationTotals.equals(100)) {
        throw new Error(`Allocations of fund "${fundId}" add up to ${allocationTotals}% instead of 100%.`);
    }
}

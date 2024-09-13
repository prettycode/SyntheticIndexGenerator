import React from 'react';
import FundSelectionTable, { UNSELECTED_FUND_FUNDID } from './Components/FundSelectionTable/FundSelectionTable';
import { FundSelectionTableState } from './Components/FundSelectionTable/FundSelectionTableState';
import './PortfolioApp.css';

const defaultTableState: FundSelectionTableState = {
    rows: [
        { fundId: UNSELECTED_FUND_FUNDID, percentage: [0, 0, 0] },
        { fundId: UNSELECTED_FUND_FUNDID, percentage: [0, 0, 0] },
        { fundId: UNSELECTED_FUND_FUNDID, percentage: [0, 0, 0] }
    ]
};

function PortfolioApp() {
    let stateToLoad: FundSelectionTableState = defaultTableState;
    const stateDeserialized: FundSelectionTableState | undefined = (() => {
        try {
            // TODO: use Zod to validate that the deserialized object is a valid FundSelectionTableState
            return JSON.parse(decodeURIComponent(location.search));
        } catch {
            return undefined;
        }
    })();

    if (stateDeserialized) {
        stateToLoad = stateDeserialized;
    } else {
        stateToLoad = {
            rows: [
                {
                    fundId: 'Custom:SPY',
                    percentage: ['30', '']
                },
                {
                    fundId: 'Custom:AVLV',
                    percentage: ['', '30']
                },
                {
                    fundId: 'Custom:AVUV',
                    percentage: ['30', '30']
                },
                {
                    fundId: 'Custom:VEA',
                    percentage: ['10', '']
                },
                {
                    fundId: 'Custom:AVIV',
                    percentage: ['', '10']
                },
                {
                    fundId: 'Custom:AVDV',
                    percentage: ['10', '10']
                },
                {
                    fundId: 'Custom:VWO',
                    percentage: ['10', '']
                },
                {
                    fundId: 'Custom:AVES',
                    percentage: ['', '10']
                },
                {
                    fundId: 'Custom:AVEE',
                    percentage: ['10', '10']
                }
            ]
        };
    }

    return (
        <div style={{ marginTop: 40 }}>
            <FundSelectionTable
                state={stateToLoad}
                onCalculatePortfolios={(rows) => {
                    const currentState: Array<FundSelectionTableState> = [{ rows }];
                    const newSearch = new URLSearchParams({ state: JSON.stringify(currentState) }).toString();
                    console.log(newSearch, JSON.stringify(currentState, null, 4));
                }}
            ></FundSelectionTable>
        </div>
    );
}

export default PortfolioApp;

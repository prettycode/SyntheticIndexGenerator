/* eslint-disable @typescript-eslint/no-unused-vars */

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
            // TODO: Validate (using Zod?) that the state is a valid FundSelectionTableState
            return JSON.parse(decodeURIComponent(location.search));
        } catch (e) {
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
                    percentage: ['20', '12', '10', '30']
                },
                {
                    fundId: 'Custom:AVLV',
                    percentage: ['20', '12', '10', '']
                },
                {
                    fundId: 'Custom:AVSC',
                    percentage: ['10', '20', '10', '']
                },
                {
                    fundId: 'Custom:AVUV',
                    percentage: ['10', '20', '10', '30']
                },
                {
                    fundId: 'Custom:VEA',
                    percentage: ['10', '', '10', '10']
                },
                {
                    fundId: 'Custom:AVIV',
                    percentage: ['10', '6', '10', '']
                },
                {
                    fundId: 'Custom:AVDS',
                    percentage: ['5', '6', '10', '']
                },
                {
                    fundId: 'Custom:AVDV',
                    percentage: ['5', '6', '10', '10']
                },
                {
                    fundId: 'Custom:VWO',
                    percentage: ['3', '4', '10', '10']
                },
                {
                    fundId: 'Custom:AVES',
                    percentage: ['3', '4', '', '5']
                },
                {
                    fundId: 'Custom:AVEE',
                    percentage: ['4', '5', '', '5']
                },
                {
                    fundId: 'Custom:VNQ',
                    percentage: ['', '5', '10', '']
                }
            ]
            /*rows: [
                {
                    fundId: 'Custom:RSST',
                    percentage: ['30', '15', '10']
                },
                {
                    fundId: 'Custom:AVEE',
                    percentage: ['15', '10', '10']
                },
                {
                    fundId: 'Custom:AVDV',
                    percentage: ['15', '10', '10']
                },
                {
                    fundId: 'Custom:KMLM',
                    percentage: ['20', '30', '30']
                },
                {
                    fundId: 'Custom:ZROZ',
                    percentage: ['10', '12.5', '10']
                },
                {
                    fundId: 'Custom:GOLD',
                    percentage: ['10', '12.5', '10']
                },
                {
                    fundId: 'Custom:SSO',
                    percentage: ['', '10', '10']
                },
                {
                    fundId: 'Custom:DBMF',
                    percentage: ['', '', '10']
                }
            ]*/
            /*rows: [
                {
                    fundId: 'Custom:RSST',
                    percentage: ['30', '20', '30']
                },
                {
                    fundId: 'Custom:VWO',
                    percentage: ['30', '20', '']
                },
                {
                    fundId: 'Custom:KMLM',
                    percentage: ['20', '20', '20']
                },
                {
                    fundId: 'Custom:ZROZ',
                    percentage: ['10', '10', '10']
                },
                {
                    fundId: 'Custom:GOLD',
                    percentage: ['10', '10', '10']
                },
                {
                    fundId: 'Custom:XLU',
                    percentage: ['', '10', '']
                },
                {
                    fundId: 'Custom:VNQ',
                    percentage: ['', '10', '']
                },
                {
                    fundId: 'Custom:AVEE',
                    percentage: ['', '', '30']
                }
            ]*/
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

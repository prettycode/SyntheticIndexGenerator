import React, { useEffect, useState } from 'react';
import './App.css';

function App() {
    const [portfolioBackTest, setPortfolioBackTest] = useState();
    const [selectedPortfolio, setSelectedPortfolio] = useState('default');
    const [periodType, setPeriodType] = useState(0); // Default to Daily
    const [rebalanceFrequency, setRebalanceFrequency] = useState(1); // Default to Annually
    const [isLoadingBackTest, setIsLoadingBackTest] = useState(true);

    const portfolioOptions = [
        { name: 'Default (AVUV 50%, VOO 50%)', value: 'default' },
        { name: 'Aggressive (AVUV 70%, VOO 30%)', value: 'aggressive' },
        { name: 'Conservative (AVUV 30%, VOO 70%)', value: 'conservative' },
    ];

    const periodOptions = [
        { name: 'Daily', value: 0 },
        { name: 'Monthly', value: 1 },
        { name: 'Yearly', value: 2 },
    ];

    const rebalanceOptions = [
        { name: 'None', value: 0 },
        { name: 'Annually', value: 1 },
        { name: 'Semi-Annually', value: 2 },
        { name: 'Quarterly', value: 3 },
        { name: 'Monthly', value: 4 },
        { name: 'Weekly', value: 5 },
        { name: 'Daily', value: 6 },
    ];

    useEffect(() => {
        setIsLoadingBackTest(true);
        (async () => {
            await fetchPortfolio(selectedPortfolio, periodType, rebalanceFrequency);
            setIsLoadingBackTest(false);
        })();
    }, [selectedPortfolio, periodType, rebalanceFrequency]);

    const handlePortfolioChange = (event) => {
        setSelectedPortfolio(event.target.value);
    };

    const handlePeriodTypeChange = (event) => {
        setPeriodType(Number(event.target.value));
    };

    const handleRebalanceFrequencyChange = (event) => {
        setRebalanceFrequency(Number(event.target.value));
    };

    const contents = portfolioBackTest === undefined
        ? <p>Loading&hellip;</p>
        : <>

            <h3>Configuration</h3>
            <div style={{ textAlign: 'left', marginBottom: '20px' }}>
                <div style={{ marginBottom: '10px' }}>
                    <label htmlFor="portfolio-select" style={{ marginRight: '10px' }}>Choose a portfolio: </label>
                    <select id="portfolio-select" value={selectedPortfolio} onChange={handlePortfolioChange}>
                        {portfolioOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
                <div style={{ marginBottom: '10px' }}>
                    <label htmlFor="period-select" style={{ marginRight: '10px' }}>Choose granularity: </label>
                    <select id="period-select" value={periodType} onChange={handlePeriodTypeChange}>
                        {periodOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
                <div>
                    <label htmlFor="rebalance-select" style={{ marginRight: '10px' }}>Choose rebalancing frequency: </label>
                    <select id="rebalance-select" value={rebalanceFrequency} onChange={handleRebalanceFrequencyChange}>
                        {rebalanceOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.name}
                            </option>
                        ))}
                    </select>
                </div>
            </div>

            <div className={isLoadingBackTest ? 'loading' : ''} >
                <h3>Overview</h3>
                <table>
                    <tbody>
                        <tr>
                            <td>First Period:</td>
                            <td>{portfolioBackTest.aggregatePerformance[0].periodStart.substr(0, 10)}</td>
                        </tr>
                        <tr>
                            <td>Last Period:</td>
                            <td>{(portfolioBackTest.aggregatePerformance[portfolioBackTest.aggregatePerformance.length - 1].periodStart.substr(0, 10))}</td>
                        </tr>
                        <tr>
                            <td>CAGR (%):</td>
                            <td>{(portfolioBackTest.cagr * 100).toFixed(2)}</td>
                        </tr>
                        <tr>
                            <td>Rebalances:</td>
                            <td>{portfolioBackTest.rebalancesByTicker[Object.keys(portfolioBackTest.rebalancesByTicker)[0]].length}</td>
                        </tr>
                        <tr>
                            <td>Years to 2x:</td>
                            <td>{portfolioBackTest.yearsBeforeDoubling.toFixed(2)}</td>
                        </tr>
                    </tbody>
                </table>

                <h3>History</h3>
                <table className='table table-striped' aria-labelledby="tableLabel">
                    <thead>
                        <tr>
                            <th>Period Start Date</th>
                            <th>Return (%)</th>
                            <th>Start Balance ($)</th>
                            <th>Ending Balance ($)</th>
                            <th>Balance Increase ($)</th>
                        </tr>
                    </thead>
                    <tbody>
                        {portfolioBackTest.aggregatePerformance.map((tick, i) =>
                            <tr key={i}>
                                <td>{tick.periodStart.substr(0, 10)}</td>
                                <td>{tick.returnPercentage.toLocaleString(undefined, {
                                    minimumFractionDigits: 2,
                                    maximumFractionDigits: 2,
                                })}</td>
                                <td>{tick.startingBalance.toLocaleString(undefined, {
                                    minimumFractionDigits: 2,
                                    maximumFractionDigits: 2,
                                })}</td>
                                <td>{tick.endingBalance.toLocaleString(undefined, {
                                    minimumFractionDigits: 2,
                                    maximumFractionDigits: 2,
                                })}</td>
                                <td>{tick.balanceIncrease.toLocaleString(undefined, {
                                    minimumFractionDigits: 2,
                                    maximumFractionDigits: 2,
                                })}</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>

        </>;

    return (
        <div>
            <h2 id="tableLabel">Portfolio Back Test</h2>
            {contents}
        </div>
    );

    async function fetchPortfolio(portfolioType, periodType, rebalanceFrequency) {
        let portfolioConstituents;
        switch (portfolioType) {
            case 'aggressive':
                portfolioConstituents = [
                    { Ticker: 'AVUV', Percentage: 70 },
                    { Ticker: 'VOO', Percentage: 30 }
                ];
                break;
            case 'conservative':
                portfolioConstituents = [
                    { Ticker: 'AVUV', Percentage: 30 },
                    { Ticker: 'VOO', Percentage: 70 }
                ];
                break;
            default:
                portfolioConstituents = [
                    { Ticker: 'AVUV', Percentage: 50 },
                    { Ticker: 'VOO', Percentage: 50 }
                ];
        }

        const response = await fetch(`https://localhost:7219/api/BackTest/GetPortfolioBackTest`, {
            method: 'POST',
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                startingBalance: 100,
                periodType,
                rebalanceStrategy: rebalanceFrequency,
                portfolioConstituents
            })
        });
        const data = await response.json();
        setPortfolioBackTest(data);
    }
}

export default App;
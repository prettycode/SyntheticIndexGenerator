import { useEffect, useState } from 'react';
import './App.css';

function App() {
    const [portfolioBackTest, setPortfolioBackTest] = useState();

    useEffect(() => {
        fetchDefaultPortfolio();
    }, []);

    const contents = portfolioBackTest === undefined
        ? <p>Loading&hellip;</p>
        : <>
            <table>
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
                    <td>{portfolioBackTest.yearsBeforeDoubling.toFixed(1)}</td>
                </tr>
            </table>
            <p/>
            <table className="table table-striped" aria-labelledby="tableLabel">
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
        </>;

    return (
        <div>
            <h2 id="tableLabel">Portfolio Back Test</h2>
            {contents}
        </div>
    );

    async function fetchDefaultPortfolio() {
        const response = await fetch(`https://localhost:7219/api/BackTest/GetPortfolioBackTest`, {
            method: 'POST',
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                startingBalance: 100,
                periodType: 1,
                rebalanceStrategy: 1,
                portfolioConstituents: [
                    { Ticker: '$^USLCB', Percentage: 50 },
                    { Ticker: '$^USSCV', Percentage: 50 }
                ]
            })
        });
        const data = await response.json();
        setPortfolioBackTest(data);
    }
}

export default App;